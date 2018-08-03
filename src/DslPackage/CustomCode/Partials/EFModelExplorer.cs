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
