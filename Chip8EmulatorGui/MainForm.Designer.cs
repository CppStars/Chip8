using System.Windows.Forms;

namespace Chip8EmulatorGui
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.programsInput = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.startButton = new System.Windows.Forms.Button();
            this.canvas = new Chip8EmulatorGui.DoubleBufferedPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.speedLevelsCombo = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // programsInput
            // 
            this.programsInput.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.programsInput.FormattingEnabled = true;
            this.programsInput.Location = new System.Drawing.Point(67, 17);
            this.programsInput.Name = "programsInput";
            this.programsInput.Size = new System.Drawing.Size(202, 21);
            this.programsInput.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Program:";
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(441, 16);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(92, 23);
            this.startButton.TabIndex = 1;
            this.startButton.Text = "Start";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.StartButtonClick);
            // 
            // canvas
            // 
            this.canvas.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.canvas.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.canvas.Location = new System.Drawing.Point(15, 59);
            this.canvas.Name = "canvas";
            this.canvas.Size = new System.Drawing.Size(640, 320);
            this.canvas.TabIndex = 0;
            this.canvas.Paint += new System.Windows.Forms.PaintEventHandler(this.CanvasPaint);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(275, 20);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Speed:";
            // 
            // speedLevelsCombo
            // 
            this.speedLevelsCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.speedLevelsCombo.FormattingEnabled = true;
            this.speedLevelsCombo.Location = new System.Drawing.Point(322, 17);
            this.speedLevelsCombo.Name = "speedLevelsCombo";
            this.speedLevelsCombo.Size = new System.Drawing.Size(109, 21);
            this.speedLevelsCombo.TabIndex = 3;
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(678, 403);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.speedLevelsCombo);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.programsInput);
            this.Controls.Add(this.canvas);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Chip8";
            this.Load += new System.EventHandler(this.MainFormLoad);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainFormKeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.MainFormKeyUp);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DoubleBufferedPanel canvas;
        private ComboBox programsInput;
        private Label label1;
        private Button startButton;
        private Label label2;
        private ComboBox speedLevelsCombo;

    }
}

