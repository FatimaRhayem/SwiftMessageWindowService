namespace SwiftMessageWindowService
{
    partial class Service1 //A partial class allows one C# class to be divided across multiple files
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null; //Visual Studio may create components that need to be cleaned up when the service is destroyed.

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) //When the service is being disposed, the cleanup method releases resources belonging to its components
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() 
        {
            components = new System.ComponentModel.Container(); //"Create the container that will keep track of the service's components."
            this.ServiceName = "Service1"; 
        }

        #endregion
    }
}


