using System; //Allows us to use basic .NET functionality, including string and StringSplitOptions-related functionality if we need it later.
using System.Collections.Generic;//Allows us to use List<string>, which will hold all the separated messages
using System.Linq;
using System.Text.RegularExpressions;

namespace SwiftMessageWindowService //Places this parser inside the same namespace as your Windows Service, so Service1 can use it later.
{
    internal class SwiftMessageParser
    {
        public List<string> SeparateMessages(string fileContent)
        {
            if (string.IsNullOrWhiteSpace(fileContent))
            {
                throw new ArgumentException(
                    "The file is empty.",
                    nameof(fileContent));
            }

            List<string> messages = fileContent.Split('$').ToList();

            return messages;
        }

         // Parses one complete SWIFT message.
        public ParsedInfo ParseMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException(
                    "The message cannot be empty",
                    nameof(message));
            }
            string block1 = ExtractBlock(message, "1");
            string block2 = ExtractBlock(message, "2");
            string block3 = ExtractBlock(message, "3");
            string block4 = ExtractBlock(message, "4");

            Dictionary<string, string> block3Fields =
                ExtractBlock3Fields(block3);

            Dictionary<string, string> block4Fields =
                ExtractBlock4Fields(block4);

            return new ParsedInfo //Create a new ParsedInfo object and fill its properties with the values we extracted
            {
                OwnBIC = ExtractOwnBIC(block1),
                CorrBIC = ExtractCorrBIC(block2),
                MessageType = ExtractMessageType(block2),
                SubFormat = ExtractSubFormat(block2),

                UserReference = GetField(block3Fields, "108"),
                SLA = GetField(block3Fields, "111"),
                UETR = GetField(block3Fields, "121"),

                Reference = GetField(block4Fields, "20"),
                OrderingCust = GetField(block4Fields, "50K"),
                BeneficiaryCust = GetField(block4Fields, "59"),
                DetailsOfCharges = GetField(block4Fields, "71A"),

                Block1 = block1, 
                Block2 = block2,
                Block3 = block3,
                Block4 = block4
            };
        }

        // Extracts one complete SWIFT block.
        private static string ExtractBlock(
            string message,
            string blockNumber)
        {
            string pattern =
                $@"\{{{blockNumber}:.*?(?=\{{[1-4]:|$)";

            Match match = Regex.Match(
                message,
                pattern,
                RegexOptions.Singleline);

            if (!match.Success)
            {
                throw new FormatException(
                    $"Block {blockNumber} was not found.");
            }

            return match.Value;
        }

        // Extracts fields such as 108, 111 and 121 from Block 3.
        private static Dictionary<string, string> ExtractBlock3Fields(string block3)
        {
            Dictionary<string, string> fields =new Dictionary<string, string>();

            MatchCollection matches = Regex.Matches(block3,@"\{(?<tag>\d{3}):(?<value>[^{}]*)\}");//Regex.Matches() → finds all matches


            foreach (Match match in matches)
            {
                string tag = match.Groups["tag"].Value;
                string value = match.Groups["value"].Value.Trim();

                fields[tag] = value;
            }

            return fields;
        }

        // Extracts fields such as 20, 50K, 59 and 71A from Block 4.
        private static Dictionary<string, string> ExtractBlock4Fields(string block4)
        {
            Dictionary<string, string> fields = new Dictionary<string, string>();

            MatchCollection matches = Regex.Matches(block4,@"(?ms)^:(?<tag>\d{2}[A-Z]?):(?<value>.*?)(?=^:\d{2}[A-Z]?:|^-\})");

            foreach (Match match in matches)
            {
                string tag = match.Groups["tag"].Value;
                string value = match.Groups["value"].Value.Trim();

                fields[tag] = value;
            }

            return fields;
        }

        // Gets a field from a dictionary.
        private static string GetField( Dictionary<string, string> fields,string tag)
        {
            return fields.TryGetValue(tag, out string value)
                ? value
                : null;
        }

        // Extracts OwnBIC from Block 1.
        private static string ExtractOwnBIC(string block1)
        {
            Match match = Regex.Match( block1,@"^\{1:F\d{2}(?<bic>[A-Z0-9]{12})");

            return match.Success
                ? match.Groups["bic"].Value
                : null;
        }

        // Extracts Correspondent BIC from Block 2.
        private static string ExtractCorrBIC(string block2)
        {
            Match inputMatch = Regex.Match(block2, @"^\{2:I\d{3}(?<bic>[A-Z0-9]{12})");

            Match outputMatch = Regex.Match(block2,@"^\{2:O\d{3}\d{10}(?<bic>[A-Z0-9]{12})");

            return inputMatch.Success
                ? inputMatch.Groups["bic"].Value
                : outputMatch.Success
                    ? outputMatch.Groups["bic"].Value
                    : null;
        }

        // Extracts the three-digit SWIFT message type.
        private static string ExtractMessageType(string block2)
        {
            Match match = Regex.Match(block2,@"^\{2:[IO](?<type>\d{3})");

            return match.Success
                ? match.Groups["type"].Value
                : null;
        }

        // Determines whether Block 2 is INPUT or OUTPUT.
        private static string ExtractSubFormat(string block2)
        {
            Match match = Regex.Match(block2,@"^\{2:(?<format>[IO])");

            return match.Success
                ? match.Groups["format"].Value == "I"
                    ? "INPUT"
                    : "OUTPUT"
                : null;
        }
    }
}

