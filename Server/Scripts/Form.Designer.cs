﻿partial class Form
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        button1 = new Button();
        richTextBox1 = new RichTextBox();
        SuspendLayout();
        // 
        // button1
        // 
        button1.Location = new Point(34, 12);
        button1.Name = "button1";
        button1.Size = new Size(75, 23);
        button1.TabIndex = 0;
        button1.Text = "Start";
        button1.UseVisualStyleBackColor = true;
        button1.Click += button1_Click;
        // 
        // richTextBox1
        // 
        richTextBox1.Location = new Point(23, 186);
        richTextBox1.Name = "richTextBox1";
        richTextBox1.Size = new Size(765, 252);
        richTextBox1.TabIndex = 1;
        richTextBox1.Text = "";
        richTextBox1.TextChanged += richTextBox1_TextChanged;
        // 
        // Form
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 450);
        Controls.Add(richTextBox1);
        Controls.Add(button1);
        Name = "Form";
        Text = "Form1";
        Load += Form_Load;
        ResumeLayout(false);
    }

    #endregion

    private Button button1;
    private RichTextBox richTextBox1;
}

