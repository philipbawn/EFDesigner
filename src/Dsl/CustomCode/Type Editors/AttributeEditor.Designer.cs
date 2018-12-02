namespace Sawczyn.EFDesigner.EFModel
{
   partial class AttributeEditor
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
         this.components = new System.ComponentModel.Container();
         this.tree = new BrightIdeasSoftware.TreeListView();
         this.NameColumn = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
         this.NamePropertyColumn = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
         ((System.ComponentModel.ISupportInitialize)(this.tree)).BeginInit();
         this.SuspendLayout();
         // 
         // tree
         // 
         this.tree.AllColumns.Add(this.NameColumn);
         this.tree.AllColumns.Add(this.NamePropertyColumn);
         this.tree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.tree.BorderStyle = System.Windows.Forms.BorderStyle.None;
         this.tree.CellEditActivation = BrightIdeasSoftware.ObjectListView.CellEditActivateMode.SingleClick;
         this.tree.CellEditUseWholeCell = false;
         this.tree.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.NameColumn,
            this.NamePropertyColumn});
         this.tree.Cursor = System.Windows.Forms.Cursors.Default;
         this.tree.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
         this.tree.Location = new System.Drawing.Point(12, 12);
         this.tree.MultiSelect = false;
         this.tree.Name = "tree";
         this.tree.PersistentCheckBoxes = false;
         this.tree.SelectColumnsMenuStaysOpen = false;
         this.tree.SelectColumnsOnRightClick = false;
         this.tree.SelectColumnsOnRightClickBehaviour = BrightIdeasSoftware.ObjectListView.ColumnSelectBehaviour.None;
         this.tree.ShowFilterMenuOnRightClick = false;
         this.tree.ShowGroups = false;
         this.tree.ShowImagesOnSubItems = true;
         this.tree.ShowSortIndicators = false;
         this.tree.Size = new System.Drawing.Size(524, 325);
         this.tree.TabIndex = 1;
         this.tree.UseCompatibleStateImageBehavior = false;
         this.tree.View = System.Windows.Forms.View.Details;
         this.tree.VirtualMode = true;
         // 
         // NameColumn
         // 
         this.NameColumn.AspectName = "Name";
         this.NameColumn.AutoCompleteEditor = false;
         this.NameColumn.AutoCompleteEditorMode = System.Windows.Forms.AutoCompleteMode.None;
         this.NameColumn.FillsFreeSpace = true;
         this.NameColumn.Groupable = false;
         this.NameColumn.Sortable = false;
         this.NameColumn.Text = "Name";
         this.NameColumn.Width = 380;
         // 
         // NamePropertyColumn
         // 
         this.NamePropertyColumn.AutoCompleteEditor = false;
         this.NamePropertyColumn.AutoCompleteEditorMode = System.Windows.Forms.AutoCompleteMode.None;
         this.NamePropertyColumn.Groupable = false;
         this.NamePropertyColumn.Sortable = false;
         this.NamePropertyColumn.Text = "Name Property";
         this.NamePropertyColumn.Width = 120;
         // 
         // AttributeEditor
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(547, 348);
         this.Controls.Add(this.tree);
         this.Name = "AttributeEditor";
         this.Text = "AttributeEditorForm";
         ((System.ComponentModel.ISupportInitialize)(this.tree)).EndInit();
         this.ResumeLayout(false);

      }

      #endregion

      private BrightIdeasSoftware.TreeListView tree;
      private BrightIdeasSoftware.OLVColumn NameColumn;
      private BrightIdeasSoftware.OLVColumn NamePropertyColumn;
   }
}