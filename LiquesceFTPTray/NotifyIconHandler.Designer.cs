﻿namespace LiquesceFTPTray
{
   partial class NotifyIconHandler
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

      #region Component Designer generated code

      /// <summary> 
      /// Required method for Designer support - do not modify 
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.components = new System.ComponentModel.Container();
         this.rightClickContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
         this.stopServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.startServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.managementApp = new System.Windows.Forms.ToolStripMenuItem();
         this.showFreeDiskSpaceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.repeatLastMessage = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
         this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.serviceController1 = new System.ServiceProcess.ServiceController();
         this.timer1 = new System.Windows.Forms.Timer(this.components);
         this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
         this.rightClickContextMenu.SuspendLayout();
         this.SuspendLayout();
         // 
         // rightClickContextMenu
         // 
         this.rightClickContextMenu.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.rightClickContextMenu.ImageScalingSize = new System.Drawing.Size(24, 24);
         this.rightClickContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.stopServiceToolStripMenuItem,
            this.startServiceToolStripMenuItem,
            this.managementApp,
            this.showFreeDiskSpaceToolStripMenuItem,
            this.repeatLastMessage,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
         this.rightClickContextMenu.Name = "rightClickContextMenu";
         this.rightClickContextMenu.Size = new System.Drawing.Size(211, 212);
         this.rightClickContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.rightClickContextMenu_Opening);
         // 
         // stopServiceToolStripMenuItem
         // 
         this.stopServiceToolStripMenuItem.Image = global::LiquesceFTPTray.Properties.Resources.Warning;
         this.stopServiceToolStripMenuItem.Name = "stopServiceToolStripMenuItem";
         this.stopServiceToolStripMenuItem.Size = new System.Drawing.Size(210, 30);
         this.stopServiceToolStripMenuItem.Text = "Stop Service";
         this.stopServiceToolStripMenuItem.ToolTipText = "This will send a \"Stop\" signal to the service";
         this.stopServiceToolStripMenuItem.Click += new System.EventHandler(this.stopServiceToolStripMenuItem_Click);
         // 
         // startServiceToolStripMenuItem
         // 
         this.startServiceToolStripMenuItem.Image = global::LiquesceFTPTray.Properties.Resources.Config;
         this.startServiceToolStripMenuItem.Name = "startServiceToolStripMenuItem";
         this.startServiceToolStripMenuItem.Size = new System.Drawing.Size(210, 30);
         this.startServiceToolStripMenuItem.Text = "Start Service";
         this.startServiceToolStripMenuItem.ToolTipText = "This will send a \"Start\" signal to the service";
         this.startServiceToolStripMenuItem.Click += new System.EventHandler(this.startServiceToolStripMenuItem_Click);
         // 
         // managementApp
         // 
         this.managementApp.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.managementApp.Image = global::LiquesceFTPTray.Properties.Resources.Liquesce;
         this.managementApp.Name = "managementApp";
         this.managementApp.Size = new System.Drawing.Size(210, 30);
         this.managementApp.Text = "&Management App..";
         this.managementApp.Click += new System.EventHandler(this.managementApp_Click);
         // 
         // showFreeDiskSpaceToolStripMenuItem
         // 
         this.showFreeDiskSpaceToolStripMenuItem.Image = global::LiquesceFTPTray.Properties.Resources.free_space;
         this.showFreeDiskSpaceToolStripMenuItem.Name = "showFreeDiskSpaceToolStripMenuItem";
         this.showFreeDiskSpaceToolStripMenuItem.Size = new System.Drawing.Size(210, 30);
         this.showFreeDiskSpaceToolStripMenuItem.Text = "Show Free Disk Space";
         this.showFreeDiskSpaceToolStripMenuItem.Click += new System.EventHandler(this.showFreeDiskSpaceToolStripMenuItem_Click);
         // 
         // repeatLastMessage
         // 
         this.repeatLastMessage.Image = global::LiquesceFTPTray.Properties.Resources.Question;
         this.repeatLastMessage.Name = "repeatLastMessage";
         this.repeatLastMessage.Size = new System.Drawing.Size(210, 30);
         this.repeatLastMessage.Text = "&Repeat Last message...";
         this.repeatLastMessage.Click += new System.EventHandler(this.repeatLastMessage_Click);
         // 
         // toolStripSeparator1
         // 
         this.toolStripSeparator1.Name = "toolStripSeparator1";
         this.toolStripSeparator1.Size = new System.Drawing.Size(207, 6);
         // 
         // exitToolStripMenuItem
         // 
         this.exitToolStripMenuItem.Image = global::LiquesceFTPTray.Properties.Resources.Stop;
         this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
         this.exitToolStripMenuItem.Size = new System.Drawing.Size(210, 30);
         this.exitToolStripMenuItem.Text = "&Exit";
         this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
         // 
         // serviceController1
         // 
         this.serviceController1.ServiceName = "LiquesceFTPSvc";
         // 
         // timer1
         // 
         this.timer1.Interval = 5000;
         this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
         // 
         // notifyIcon1
         // 
         this.notifyIcon1.ContextMenuStrip = this.rightClickContextMenu;
         this.notifyIcon1.Icon = global::LiquesceFTPTray.Properties.Resources.LiquesceIcon;
         this.notifyIcon1.Text = "Liquesce FTP Starting up";
         this.notifyIcon1.Visible = true;
         this.notifyIcon1.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
         // 
         // NotifyIconHandler
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.Name = "NotifyIconHandler";
         this.Size = new System.Drawing.Size(175, 162);
         this.rightClickContextMenu.ResumeLayout(false);
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.ContextMenuStrip rightClickContextMenu;
      private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
      internal System.Windows.Forms.NotifyIcon notifyIcon1;
      private System.Windows.Forms.ToolStripMenuItem managementApp;
      private System.Windows.Forms.ToolStripMenuItem repeatLastMessage;
      private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
      private System.ServiceProcess.ServiceController serviceController1;
      private System.Windows.Forms.Timer timer1;
      private System.Windows.Forms.ToolStripMenuItem stopServiceToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem startServiceToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem showFreeDiskSpaceToolStripMenuItem;
   }
}
