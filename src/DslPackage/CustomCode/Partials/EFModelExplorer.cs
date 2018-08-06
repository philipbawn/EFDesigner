using System.ComponentModel.Design;
using System.Linq;
using System.Windows.Forms;

using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Shell;

namespace Sawczyn.EFDesigner.EFModel
{
   internal partial class EFModelExplorer
   {
      protected override IElementVisitor CreateElementVisitor()
      {
         return new EFModelExplorerElementVisitor(this);
      }

      // following code derived from http://www.ticklishtechs.net/2008/05/15/preventing-model-elements-from-being-deleted/

      protected override void ProcessOnStatusDeleteCommand(MenuCommand command)
      {
         if (command == null) return;

         base.ProcessOnStatusDeleteCommand(command);
         
         ModelElement selectedElement = SelectedElement;
         ProcessElement(command, selectedElement);
      }

      protected override void ProcessOnStatusDeleteAllCommand(MenuCommand command)
      {
         base.ProcessOnStatusDeleteAllCommand(command);

         if (command.Enabled || command.Visible)
         {
            ExplorerTreeNode selectedNode = ObjectModelBrowser.SelectedNode as ExplorerTreeNode;
            ModelElement selectedElement = selectedNode?.RepresentedElement;
            ProcessElement(command, selectedElement);

            foreach (ExplorerTreeNode node in selectedNode.Nodes.OfType<ExplorerTreeNode>())
               ProcessElement(command, node.RepresentedElement);
         }
      }

      private static void ProcessElement(MenuCommand command, ModelElement selectedElement)
      {
         bool isDefaultDiagram = selectedElement != null && selectedElement is ModelView view && view.Name == "Default";
         command.Enabled &= !isDefaultDiagram;
         command.Visible &= !isDefaultDiagram;
      }

      public override void AddCommandHandlers(IMenuCommandService menuCommandService)
      {
         base.AddCommandHandlers(menuCommandService);

         MenuCommand deleteAllCommand = menuCommandService.FindCommand(CommonModelingCommands.ModelExplorerDeleteAll);
         ObjectModelBrowser.AfterSelect += (sender, args) => ProcessOnStatusDeleteAllCommand(deleteAllCommand);
      }
   }

   public class EFModelExplorerElementVisitor : ExplorerElementVisitor
   {
      private readonly EFModelExplorer treeContainer;

      public EFModelExplorerElementVisitor(ModelExplorerTreeContainer treeContainer) : base(treeContainer)
      {
         this.treeContainer = (EFModelExplorer)treeContainer;
      }

      public override void EndTraverse(ElementWalker walker)
      {
         base.EndTraverse(walker);
      }


      public override void StartTraverse(ElementWalker walker)
      {
         base.StartTraverse(walker);
      }

      public override bool Visit(ElementWalker walker, ModelElement modelElement)
      {
         return base.Visit(walker, modelElement);
      }
   }

}
