﻿/*
 Copyright (c) 2015 [Joerg Ruedenauer]
 
 This file is part of Ares.

 Ares is free software; you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation; either version 2 of the License, or
 (at your option) any later version.

 Ares is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with Ares; if not, write to the Free Software
 Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */
namespace Ares.CommonGUI
{
    partial class SettingsDialog
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsDialog));
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.generalPage = new System.Windows.Forms.TabPage();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.updateCheckBox = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.soundDirLabel = new Ares.CommonGUI.PathLabel();
            this.musicDirLabel = new Ares.CommonGUI.PathLabel();
            this.selectSoundButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.selectMusicButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.selectOtherDirButton = new System.Windows.Forms.Button();
            this.otherDirLabel = new Ares.CommonGUI.PathLabel();
            this.otherDirButton = new System.Windows.Forms.RadioButton();
            this.appDirLabel = new Ares.CommonGUI.PathLabel();
            this.appDirButton = new System.Windows.Forms.RadioButton();
            this.userDirLabel = new Ares.CommonGUI.PathLabel();
            this.userDirButton = new System.Windows.Forms.RadioButton();
            this.tabControl.SuspendLayout();
            this.generalPage.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // folderBrowserDialog
            // 
            this.folderBrowserDialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
            // 
            // cancelButton
            // 
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.UseCompatibleTextRendering = true;
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Name = "okButton";
            this.okButton.UseCompatibleTextRendering = true;
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // tabControl
            // 
            resources.ApplyResources(this.tabControl, "tabControl");
            this.tabControl.Controls.Add(this.generalPage);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            // 
            // generalPage
            // 
            this.generalPage.BackColor = System.Drawing.SystemColors.Control;
            this.generalPage.Controls.Add(this.groupBox3);
            this.generalPage.Controls.Add(this.groupBox2);
            this.generalPage.Controls.Add(this.groupBox1);
            resources.ApplyResources(this.generalPage, "generalPage");
            this.generalPage.Name = "generalPage";
            // 
            // groupBox3
            // 
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Controls.Add(this.updateCheckBox);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            this.groupBox3.UseCompatibleTextRendering = true;
            // 
            // updateCheckBox
            // 
            resources.ApplyResources(this.updateCheckBox, "updateCheckBox");
            this.updateCheckBox.Name = "updateCheckBox";
            this.updateCheckBox.UseCompatibleTextRendering = true;
            this.updateCheckBox.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.soundDirLabel);
            this.groupBox2.Controls.Add(this.musicDirLabel);
            this.groupBox2.Controls.Add(this.selectSoundButton);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.selectMusicButton);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            this.groupBox2.UseCompatibleTextRendering = true;
            // 
            // soundDirLabel
            // 
            resources.ApplyResources(this.soundDirLabel, "soundDirLabel");
            this.soundDirLabel.Name = "soundDirLabel";
            this.soundDirLabel.UseCompatibleTextRendering = true;
            // 
            // musicDirLabel
            // 
            resources.ApplyResources(this.musicDirLabel, "musicDirLabel");
            this.musicDirLabel.Name = "musicDirLabel";
            this.musicDirLabel.UseCompatibleTextRendering = true;
            // 
            // selectSoundButton
            // 
            resources.ApplyResources(this.selectSoundButton, "selectSoundButton");
            this.selectSoundButton.Name = "selectSoundButton";
            this.selectSoundButton.UseCompatibleTextRendering = true;
            this.selectSoundButton.UseVisualStyleBackColor = true;
            this.selectSoundButton.Click += new System.EventHandler(this.selectSoundButton_Click);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            this.label2.UseCompatibleTextRendering = true;
            // 
            // selectMusicButton
            // 
            resources.ApplyResources(this.selectMusicButton, "selectMusicButton");
            this.selectMusicButton.Name = "selectMusicButton";
            this.selectMusicButton.UseCompatibleTextRendering = true;
            this.selectMusicButton.UseVisualStyleBackColor = true;
            this.selectMusicButton.Click += new System.EventHandler(this.selectMusicButton_Click);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            this.label1.UseCompatibleTextRendering = true;
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.selectOtherDirButton);
            this.groupBox1.Controls.Add(this.otherDirLabel);
            this.groupBox1.Controls.Add(this.otherDirButton);
            this.groupBox1.Controls.Add(this.appDirLabel);
            this.groupBox1.Controls.Add(this.appDirButton);
            this.groupBox1.Controls.Add(this.userDirLabel);
            this.groupBox1.Controls.Add(this.userDirButton);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            this.groupBox1.UseCompatibleTextRendering = true;
            // 
            // selectOtherDirButton
            // 
            resources.ApplyResources(this.selectOtherDirButton, "selectOtherDirButton");
            this.selectOtherDirButton.Name = "selectOtherDirButton";
            this.selectOtherDirButton.UseCompatibleTextRendering = true;
            this.selectOtherDirButton.UseVisualStyleBackColor = true;
            this.selectOtherDirButton.Click += new System.EventHandler(this.selectOtherDirButton_Click);
            // 
            // otherDirLabel
            // 
            resources.ApplyResources(this.otherDirLabel, "otherDirLabel");
            this.otherDirLabel.Name = "otherDirLabel";
            this.otherDirLabel.UseCompatibleTextRendering = true;
            // 
            // otherDirButton
            // 
            resources.ApplyResources(this.otherDirButton, "otherDirButton");
            this.otherDirButton.Name = "otherDirButton";
            this.otherDirButton.TabStop = true;
            this.otherDirButton.UseCompatibleTextRendering = true;
            this.otherDirButton.UseVisualStyleBackColor = true;
            // 
            // appDirLabel
            // 
            resources.ApplyResources(this.appDirLabel, "appDirLabel");
            this.appDirLabel.Name = "appDirLabel";
            this.appDirLabel.UseCompatibleTextRendering = true;
            // 
            // appDirButton
            // 
            resources.ApplyResources(this.appDirButton, "appDirButton");
            this.appDirButton.Name = "appDirButton";
            this.appDirButton.TabStop = true;
            this.appDirButton.UseCompatibleTextRendering = true;
            this.appDirButton.UseVisualStyleBackColor = true;
            // 
            // userDirLabel
            // 
            resources.ApplyResources(this.userDirLabel, "userDirLabel");
            this.userDirLabel.Name = "userDirLabel";
            this.userDirLabel.UseCompatibleTextRendering = true;
            // 
            // userDirButton
            // 
            resources.ApplyResources(this.userDirButton, "userDirButton");
            this.userDirButton.Name = "userDirButton";
            this.userDirButton.TabStop = true;
            this.userDirButton.UseCompatibleTextRendering = true;
            this.userDirButton.UseVisualStyleBackColor = true;
            // 
            // SettingsDialog
            // 
            this.AcceptButton = this.okButton;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsDialog";
            this.ShowInTaskbar = false;
            this.tabControl.ResumeLayout(false);
            this.generalPage.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage generalPage;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.CheckBox updateCheckBox;
        private System.Windows.Forms.GroupBox groupBox2;
        private PathLabel soundDirLabel;
        private PathLabel musicDirLabel;
        private System.Windows.Forms.Button selectSoundButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button selectMusicButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button selectOtherDirButton;
        private PathLabel otherDirLabel;
        private System.Windows.Forms.RadioButton otherDirButton;
        private PathLabel appDirLabel;
        private System.Windows.Forms.RadioButton appDirButton;
        private PathLabel userDirLabel;
        private System.Windows.Forms.RadioButton userDirButton;
    }
}