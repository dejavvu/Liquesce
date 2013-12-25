﻿#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="Edit.cs" company="Smurf-IV">
// 
//  Copyright (C) 2013 Simon Coghlan (Aka Smurf-IV)
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 2 of the License, or
//   any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program. If not, see http://www.gnu.org/licenses/.
//  </copyright>
//  <summary>
//  Url: http://Liquesce.codeplex.com/
//  Email: http://www.codeplex.com/site/users/view/smurfiv
//  </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using Liquesce.Tabs;
using LiquesceFacade;
using NLog;

namespace Liquesce.Mounting
{
   public partial class Edit : UserControl, ITab
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private bool isClosing;

      public Edit()
      {
         InitializeComponent();

         Icon icon = ExtractIcon.GetIconForFilename(Environment.GetFolderPath(Environment.SpecialFolder.MyComputer), true);
         imageListUnits.Images.Add("MyComputer", icon.ToBitmap());
      }


      public ConfigDetails cd
      {
         set
         {
            cd1 = value;
            UseWaitCursor = true;
            PopulatePoolSettings();
            InitialiseWith();
            UseWaitCursor = false;
         }
         private get { return cd1; }
      }

      private bool IsClosing
      {
         get { return isClosing; }

      }

      private void InitialiseWith()
      {
         MountDetail mt = cd.MountDetails[currentIndex];
         if (!String.IsNullOrWhiteSpace(mt.DriveLetter))
         {
            // Add the drive letter back in as this would already have been removed
            MountPoint.Items.Add(mt.DriveLetter);
            MountPoint.Text = mt.DriveLetter;
            if (mt.DriveLetter.Length > 1)
            {
               txtFolder.Text = mt.DriveLetter;
            }
            if (mt.SourceLocations != null)
            {
               foreach (SourceLocation tn in mt.SourceLocations)
               {
                  mergeList.Rows.Add(new object[] { tn.SourcePath, tn.UseAsNameRoot, tn.UseIsReadOnly});
               }
            }
            VolumeLabel.Text = mt.VolumeLabel;
            AllocationMode = mt.AllocationMode;
            HoldOffMBytes = mt.HoldOffBufferBytes;
            RestartExpectedOutput();
         }
      }

      private void PopulatePoolSettings()
      {
         string[] drives = Environment.GetLogicalDrives();
         foreach (string dr in drives)
         {
            MountPoint.Items.Remove(dr.Remove(1));
         }
         MountPoint.Text = "N";
      }

      #region Methods to populate and drill down in the tree
      // Code stolen from the http://frwingui.codeplex.com project
      private void StartTree()
      {
         // Code taken and adapted from http://msdn.microsoft.com/en-us/library/bb513869.aspx
         try
         {
            Enabled = false;
            UseWaitCursor = true;
            driveAndDirTreeView.Nodes.Clear();

            Log.Debug("Create the root node.");
            TreeNode tvwRoot = new TreeNode
            {
               Text = Environment.MachineName,
               ImageKey = "MyComputer",
               SelectedImageKey = "MyComputer"
            };
            driveAndDirTreeView.Nodes.Add(tvwRoot);
            Log.Debug("Now we need to add any children to the root node.");

            Log.Debug("Start with drives if you have to search the entire computer.");
            string[] drives = Environment.GetLogicalDrives();
            foreach (string dr in drives)
            {
               Log.Debug(dr);
               DriveInfo di = new DriveInfo(dr);
               FillInDirectoryType(tvwRoot, di);
            }

            tvwRoot.Expand();
         }
         catch (Exception ex)
         {
            Log.ErrorException("StartTree Threw: ", ex);
         }
         finally
         {
            Enabled = true;
            UseWaitCursor = false;
         }
      }

      private void FillInDirectoryType(TreeNode parentNode, DriveInfo di)
      {
         if (di != null)
         {
            string label;
            string di_DriveFormat = "Unknown Format";
            try
            {
               di_DriveFormat = di.DriveFormat;
               if (di_DriveFormat.ToUpper() == "FAT")
               {
                  Log.Warn("Removing FAT formated drive type, as this causes ACL Failures [{0}]", di.Name);
                  return;
               }
               if (di.VolumeLabel == "Liquesce")
               {
                  Log.Warn("Removing the existing CBFS drive as this would cause confusion ! [{0}]",
                     di.Name);
                  return;
               }
               SafelyAddIcon(di.Name);
               label = (di.IsReady && !String.IsNullOrWhiteSpace(di.VolumeLabel)) ? di.VolumeLabel : di_DriveFormat;
            }
            catch
            {
               // The above throws a wobble e.g. if the CD_Rom does not have a disk in it
               label = di_DriveFormat;
            }
            label += " (" + di.Name + ")";
            TreeNode thisNode = new TreeNode
            {
               Text = label,
               ImageKey = di.Name,
               SelectedImageKey = di.Name,
               Tag = di.RootDirectory
            };
            if (di.IsReady)
            {
               thisNode.Nodes.Add("DummyNode");
            }
            parentNode.Nodes.Add(thisNode);
         }
      }

      private void WalkNextTreeLevel(TreeNode parentNode)
      {
         try
         {
            DirectoryInfo root = parentNode.Tag as DirectoryInfo;
            if (root != null)
            {
               Log.Debug("// Find all the subdirectories under this directory.");
               DirectoryInfo[] subDirs = root.GetDirectories();
               // if (subDirs != null) // Apperently always true
               {
                  foreach (DirectoryInfo dirInfo in subDirs)
                  {
                     // Recursive call for each subdirectory.
                     SafelyAddIcon(dirInfo.FullName);
                     TreeNode tvwChild = new TreeNode
                     {
                        Text = dirInfo.Name,
                        ImageKey = dirInfo.FullName,
                        SelectedImageKey = dirInfo.FullName,
                        Tag = dirInfo
                     };

                     Log.Debug("If this is a folder item and has children then add a place holder node.");
                     try
                     {
                        DirectoryInfo[] subChildDirs = dirInfo.GetDirectories();
                        if (/*(subChildDirs != null) // Apperently always true
                            &&*/ (subChildDirs.Length > 0)
                           )
                           tvwChild.Nodes.Add("DummyNode");
                     }
                     catch (UnauthorizedAccessException uaex)
                     {
                        Log.InfoException("No Access to subdirs in " + tvwChild.Text, uaex);
                     }
                     parentNode.Nodes.Add(tvwChild);
                  }
               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("RecurseAddChildren has thrown:", ex);
         }
      }

      private void driveAndDirTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
      {
         Enabled = false;
         UseWaitCursor = true;
         try
         {
            Log.Debug("driveAndDirTreeView_BeforeExpand");
            DirectoryInfo root = e.Node.Tag as DirectoryInfo;
            if (root != null)
            {
               e.Node.Nodes.Clear();
               WalkNextTreeLevel(e.Node);
            }
            e.Cancel = false;
         }
         catch (Exception ex)
         {
            Log.ErrorException("BeforeExpand has thrown: ", ex);
         }
         finally
         {
            Enabled = true;
            UseWaitCursor = false;
         }
      }
      #endregion

      #region DragAndDrop over to the merge list
      private void driveAndDirTreeView_MouseDown(object sender, MouseEventArgs e)
      {
         if (e.Button == MouseButtons.Left)
         {
            // Get the node underneath the mouse.
            TreeNode selected = driveAndDirTreeView.GetNodeAt(e.X, e.Y);
            driveAndDirTreeView.SelectedNode = selected;

            // Start the drag-and-drop operation with a cloned copy of the node.
            if (selected != null)
            {
               DragDropItem ud = new DragDropItem(GetSelectedNodesPath(selected), DragDropItem.SourceType.Drive);
               if (!String.IsNullOrEmpty(ud.Name))
                  driveAndDirTreeView.DoDragDrop(ud, DragDropEffects.All);
            }
         }
      }

      private string GetSelectedNodesPath(TreeNode selected)
      {
         DirectoryInfo shNode = selected.Tag as DirectoryInfo;
         Log.Debug("Now we need to add any children to the root node.");
         string newPath = shNode != null ? shNode.FullName : selected.FullPath;
         return newPath;
      }

      private void CheckDrop(DragDropItem newPath, int target, int mouseOffset)
      {
         // TODO: On Add check to make sure that the root (Or this) node have not already been covered.
         if (!String.IsNullOrEmpty(newPath.Name))
         {
            Object[] tn = {newPath.Name, false, false};

            //we only ever want an entry in 1x in the list.  Remove any duplicates, so you can reorder from the filesystem treeview
            int internalMoveIndex = -1;
            foreach (DataGridViewRow row in mergeList.Rows)
            {
               if (row.Cells[0].Value.ToString().Equals(newPath.Name))
               {
                  internalMoveIndex = row.Index;
                  break;
               }
            }
            int index = 0;
            if (internalMoveIndex > -1)
            {
               // no node below?  stick this at the Top of the list.
               if (target == -1)
               {
                  index = 0;
                  if (mouseOffset > mergeList.Rows[0].Height)
                  {
                     index = mergeList.RowCount - 1;
                  }
               }
               else
               {
                  index = target;
               }
               if (internalMoveIndex < index)
               {
                  mergeList.Rows.Insert(index + 1, tn);
                  mergeList.Rows.RemoveAt(internalMoveIndex);
               }
               else if (internalMoveIndex > index)
               {
                  mergeList.Rows.RemoveAt(internalMoveIndex);
                  mergeList.Rows.Insert(index, tn);
               }
            }
            else if (newPath.Source == DragDropItem.SourceType.Drive)
            {
               //no node below?  stick this at the bottom of the list, else put in before the one your over.
               if (target == -1)
               {
                  index = 0;
                  if (mouseOffset > mergeList.Rows[0].Height)
                  {
                     index = mergeList.RowCount;
                  }
               }
               else
               {
                  index = target;
               }
               mergeList.Rows.Insert(index, tn);
            }
            mergeList.Rows[index].Selected = true;
            RestartExpectedOutput();
         }
      }


      #endregion

      private void RestartExpectedOutput()
      {
         if (IsClosing)
         {
            return;
         }
         FillExpectedLayoutWorker.CancelAsync();
         while (FillExpectedLayoutWorker.IsBusy)
         {
            Thread.Sleep(500);
            Application.DoEvents();
         }
         MountDetail mt = new MountDetail
         {
            DriveLetter = string.IsNullOrWhiteSpace(txtFolder.Text) ? MountPoint.Text : txtFolder.Text,
            VolumeLabel = VolumeLabel.Text,
            SourceLocations = (from DataGridViewRow tn in mergeList.Rows
                               select new SourceLocation(tn.Cells[0].Value.ToString(), (bool) tn.Cells[1].Value, (bool) tn.Cells[2].Value))
               .ToList(),
         };
         FillExpectedLayoutWorker.RunWorkerAsync(mt);
      }


      private void FillExpectedLayoutWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
      {
         SetProgressBarStyle(ProgressBarStyle.Continuous);
      }

      private void FillExpectedLayoutWorker_DoWork(object sender, DoWorkEventArgs e)
      {
         SetProgressBarStyle(ProgressBarStyle.Marquee);
         ClearExpectedList();
         MountDetail mt = e.Argument as MountDetail;
         BackgroundWorker worker = sender as BackgroundWorker;
         if ((mt == null)
            || (worker == null)
            )
         {
            Log.Error("Worker, or auguments are null, exiting.");
            return;
         }
         TreeNode root = new TreeNode(string.Format("{0} ({1})", mt.VolumeLabel, mt.DriveLetter));
         AddExpectedNode(null, root);
         if (worker.CancellationPending
            || IsClosing)
         {
            return;
         }
         WalkExpectedNextTreeLevel(root, mt.SourceLocations);
      }


      private void AddFiles(List<SourceLocation> sourceLocations, string directoryPath, List<ExpectedDetailResult> allFiles)
      {
         try
         {
            DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);
            if (dirInfo.Exists)
            {
               FileSystemInfo[] fileSystemInfos = dirInfo.GetFileSystemInfos();
               allFiles.AddRange(fileSystemInfos.Select(info2 => new ExpectedDetailResult
               {
                  DisplayName = TrimAndAdd(sourceLocations, info2.FullName),
                  ActualFileLocation = info2.FullName
               }));
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("AddFiles threw: ", ex);
         }
      }

      private string TrimAndAdd(List<SourceLocation> sourceLocations, string fullFilePath)
      {
         foreach (string key in from location 
                                   in sourceLocations 
                                where fullFilePath.StartsWith(location.SourcePath) 
                                select fullFilePath.Remove(0, location.SourcePath.Length)
                   )
         {
            return key;
         }
         throw new ArgumentException("Unable to find BelongTo Path: " + fullFilePath, fullFilePath);
      }


      private delegate void AddExpecteddNodeCallBack(TreeNode parent, TreeNode child);
      private void AddExpectedNode(TreeNode parent, TreeNode child)
      {
         if (expectedTreeView.InvokeRequired)
         {
            AddExpecteddNodeCallBack d = AddExpectedNode;
            BeginInvoke(d, new object[] { parent, child });
         }
         else
         {
            if (parent == null)
            {
               expectedTreeView.Nodes.Add(child);
            }
            else
            {
               parent.Nodes.Add(child);
            }
         }
      }

      delegate void SetProgressBarStyleCallback(ProgressBarStyle style);
      private void SetProgressBarStyle(ProgressBarStyle style)
      {
         // InvokeRequired required compares the thread ID of the
         // calling thread to the thread ID of the creating thread.
         // If these threads are different, it returns true.
         if (progressBar1.InvokeRequired)
         {
            SetProgressBarStyleCallback d = SetProgressBarStyle;
            BeginInvoke(d, new object[] { style });
         }
         else
         {
            progressBar1.Style = style;
            expectedTreeView.Enabled = (style != ProgressBarStyle.Marquee);
            UseWaitCursor = !Enabled;
         }
      }

      private delegate void ClearExpectedListCallBack();
      private void ClearExpectedList()
      {
         if (expectedTreeView.InvokeRequired)
         {
            ClearExpectedListCallBack d = ClearExpectedList;
            BeginInvoke(d);
         }
         else
         {
            expectedTreeView.Nodes.Clear();
         }
      }

      private void expectedTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
      {
         try
         {
            SetProgressBarStyle(ProgressBarStyle.Marquee);
            Log.Debug("expectedTreeView_BeforeExpand");
            string root = e.Node.Tag as string;
            if (!String.IsNullOrEmpty(root))
            {
               e.Node.Nodes.Clear();
               List<SourceLocation> sourceLocations = new List<SourceLocation>(mergeList.RowCount);
               sourceLocations.AddRange(from DataGridViewRow tn in mergeList.Rows select new SourceLocation(tn.Cells[0].Value.ToString(), (bool) tn.Cells[1].Value, (bool) tn.Cells[2].Value));
               WalkExpectedNextTreeLevel(e.Node, sourceLocations, root);
            }
            e.Cancel = false;
         }
         catch (Exception ex)
         {
            Log.ErrorException("BeforeExpand has thrown: ", ex);
         }
         finally
         {
            SetProgressBarStyle(ProgressBarStyle.Continuous);
         }
      }

      private void WalkExpectedNextTreeLevel(TreeNode parent, List<SourceLocation> sourceLocations)
      {
         WalkExpectedNextTreeLevel(parent, sourceLocations, String.Empty);
      }

      private void WalkExpectedNextTreeLevel(TreeNode parent, List<SourceLocation> sourceLocations, string expectedStartLocation)
      {
         List<ExpectedDetailResult> allFiles = new List<ExpectedDetailResult>();
         if (sourceLocations != null)
         {
            sourceLocations.ForEach(str2 => AddFiles(sourceLocations, str2.SourcePath + expectedStartLocation, allFiles));
         }
         allFiles.Sort();
         Log.Debug("Should now have a huge list of filePaths");
         AddNextExpectedLevel(allFiles, parent);
      }

      private void AddNextExpectedLevel(List<ExpectedDetailResult> allFiles, TreeNode parent)
      {
         if (allFiles != null)
         {
            foreach (ExpectedDetailResult kvp in allFiles)
            {
               if (IsClosing)
               {
                  return;
               }
               if (Directory.Exists(kvp.ActualFileLocation))
               {
                  // This is a Dir, so make a new child
                  string label = kvp.DisplayName;
                  int index = kvp.DisplayName.LastIndexOf(Path.DirectorySeparatorChar);
                  if (index > 0)
                  {
                     label = kvp.DisplayName.Substring(index + 1);
                  }
                  bool found = parent.Nodes.Cast<TreeNode>().Any(node => node.Text == label);
                  if (!found)
                  {
                     TreeNode child = new TreeNode
                     {
                        Text = label,
                        Tag = kvp.DisplayName,
                        ToolTipText = kvp.ActualFileLocation
                     };
                     child.Nodes.Add("DummyNode");
                     AddExpectedNode(parent, child);
                  }
               }
               else
               {
                  AddFileNodeCallBack d = AddFileNode;
                  BeginInvoke(d, new object[] { kvp, parent });
               }
            }
         }
      }

      private delegate void AddFileNodeCallBack(ExpectedDetailResult kvp, TreeNode parent);

      private void AddFileNode(ExpectedDetailResult kvp, TreeNode parent)
      {
         SafelyAddIcon(kvp.ActualFileLocation);
         TreeNode child = new TreeNode
         {
            Text = Path.GetFileName(kvp.DisplayName),
            ImageKey = kvp.ActualFileLocation,
            ToolTipText = kvp.ActualFileLocation
         };
         AddExpectedNode(parent, child);
      }

      private void SafelyAddIcon(string fullFileName)
      {
         try
         {
            if (!imageListUnits.Images.ContainsKey(fullFileName))
            {
               imageListUnits.Images.Add(fullFileName, ExtractIcon.GetIconForFilename(fullFileName, true).ToBitmap());
            }
         }
         catch { }
      }

      private void MountPoint_TextChanged(object sender, EventArgs e)
      {
         txtFolder.Text = (MountPoint.Text.Length > 1) ? MountPoint.Text : string.Empty;
         RestartExpectedOutput();
      }

      private void VolumeLabel_Validated(object sender, EventArgs e)
      {
         RestartExpectedOutput();
      }

      private void refreshExpectedToolStripMenuItem_Click(object sender, EventArgs e)
      {
         RestartExpectedOutput();
      }

      private void txtFolder_DragDrop(object sender, DragEventArgs e)
      {
         // Is it a valid format?
         DragDropItem ud = e.Data.GetData(typeof(DragDropItem)) as DragDropItem;
         if (ud != null)
         {
            txtFolder.Text = ud.Name;
         }
      }

      private void txtFolder_DragOver(object sender, DragEventArgs e)
      {
         // Drag and drop denied by default.
         e.Effect = DragDropEffects.None;

         // Is it a valid format?
         DragDropItem ud = e.Data.GetData(typeof(DragDropItem)) as DragDropItem;
         if (ud != null)
         {
            e.Effect = DragDropEffects.Copy;
         }
      }

      private readonly List<string> hardDrives = (from dr in Environment.GetLogicalDrives()
                                                  let di = new DriveInfo(dr)
                                                  where di.DriveType == DriveType.Fixed
                                                  select dr).ToList();

      private ConfigDetails cd1;

      private void txtFolder_TextChanged(object sender, EventArgs e)
      {
         string error = string.Empty;
         try
         {
            if (!string.IsNullOrWhiteSpace(txtFolder.Text))
            {
               DirectoryInfo dir = new DirectoryInfo(txtFolder.Text);
               if (!hardDrives.Contains(dir.Root.Name.ToUpperInvariant()))
               {
                  error = @"Drive does not exist";
               }
               else if (!dir.Exists)
               {
                  error = string.Empty;
               }
               else if (!MountPoint.Items.Contains(txtFolder.Text) // may already be mounted
                  && dir.EnumerateFileSystemInfos().Any()
                  )
               {
                  error = @"Directory is not empty";
               }
            }
         }
         catch (Exception ex)
         {
            error = ex.Message;
         }
         errorProvider1.SetError(lblFolder, error);
      }

      private void MountingPoints_Load(object sender, EventArgs e)
      {
         StartTree();
      }

      private void MountingPoints_Leave(object sender, EventArgs e)
      {
         isClosing = true;
         FillExpectedLayoutWorker.CancelAsync();
         MountDetail mt = new MountDetail
         {
            DriveLetter = string.IsNullOrWhiteSpace(txtFolder.Text) ? MountPoint.Text : txtFolder.Text,
            VolumeLabel = VolumeLabel.Text,
            SourceLocations = (from DataGridViewRow tn in mergeList.Rows
                               select new SourceLocation(tn.Cells[0].Value.ToString(), (bool)tn.Cells[1].Value, (bool)tn.Cells[2].Value))
               .ToList(),
            HoldOffBufferBytes = HoldOffMBytes,
            AllocationMode = AllocationMode
         };
         cd.MountDetails[currentIndex] = mt;
      }

      private void dataGridView1_DragDrop(object sender, DragEventArgs e)
      {
         // Is it a valid format?
         DragDropItem ud = e.Data.GetData(typeof(DragDropItem)) as DragDropItem;
         if (ud != null)
         {
            Point newPoint = mergeList.PointToClient(new Point(e.X, e.Y));
            DataGridView.HitTestInfo hti = mergeList.HitTest(newPoint.X, newPoint.Y);
            CheckDrop(ud, hti.RowIndex, newPoint.Y);
         }
      }

      private void dataGridView1_DragOver(object sender, DragEventArgs e)
      {
         // Drag and drop denied by default.
         e.Effect = DragDropEffects.None;

         // Is it a valid format?
         DragDropItem ud = e.Data.GetData(typeof(DragDropItem)) as DragDropItem;
         if (ud != null)
         {
            //we only ever want an entry in 1x in the list.  Remove any duplicates, so you can reorder from the filesystem treeview
            if (ud.Source == DragDropItem.SourceType.Drive)
            {
               bool found = mergeList.Rows.Cast<DataGridViewRow>().Any(row => row.Cells[0].Value.ToString() == ud.Name);
               if (!found)
               {
                  e.Effect = DragDropEffects.Copy;
               }
            }
            else
            {
               e.Effect = DragDropEffects.Copy;
            }
         }
      }

      private void mergeListContextMenuItem_Click(object sender, EventArgs e)
      {
         foreach (DataGridViewRow item in mergeList.SelectedRows)
         {
            mergeList.Rows.RemoveAt(item.Index);
         }
         RestartExpectedOutput();
      }

      private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
      {
         DataGridView.HitTestInfo hti = mergeList.HitTest(e.X, e.Y);
         mergeList.ClearSelection();
         if (hti.RowIndex < 0)
         {
            return;
         }
         mergeList.Rows[hti.RowIndex].Selected = true;

         //right click menu for deleting items from mergelist
         if (e.Button == MouseButtons.Right)
         {
            mergeList.ContextMenuStrip.Show(mergeList, e.Location);
         }
         //enable dragging for mergelist
         else if (e.Button == MouseButtons.Left)
         {
            // Get the node underneath the mouse.
            // Start the drag-and-drop operation with a cloned copy of the node.
            DragDropItem ud = new DragDropItem(mergeList.Rows[hti.RowIndex].Cells[0].Value.ToString(), DragDropItem.SourceType.Merge);
            mergeList.DoDragDrop(ud, DragDropEffects.All);
         }
      }

      private void mergeList_KeyUp(object sender, KeyEventArgs e)
      {
         if (e.KeyCode != Keys.Delete)
         {
            return;
         }
         foreach (DataGridViewRow item in mergeList.SelectedRows)
         {
            mergeList.Rows.RemoveAt(item.Index);
         }
         RestartExpectedOutput();
      }

      private int currentIndex = 0;
      public void SelectedIndex(int selectedIndex)
      {
         currentIndex = selectedIndex;
      }

      public MountDetail.AllocationModes AllocationMode
      {
         get
         {
            Enum.TryParse(cmbAllocationMode.Text, out cd.MountDetails[currentIndex].AllocationMode);
            return cd.MountDetails[currentIndex].AllocationMode;
         }
         set 
         { 
            cmbAllocationMode.Text = cd.MountDetails[currentIndex].AllocationMode.ToString(); 
         }
      }

      public ulong HoldOffMBytes
      {
         get 
         {
            cd.MountDetails[currentIndex].HoldOffBufferBytes = (ulong) (numHoldOffBytes.Value * (1024 * 1024));
            return cd.MountDetails[currentIndex].HoldOffBufferBytes; 
         }
         set 
         {
            numHoldOffBytes.Value = cd.MountDetails[currentIndex].HoldOffBufferBytes / (decimal)(1024 * 1024);
         }
      }

      private void cmbAllocationMode_SelectedValueChanged(object sender, EventArgs e)
      {
         numHoldOffBytes.Enabled = (cmbAllocationMode.Text != "Balanced");
      }

   }
}