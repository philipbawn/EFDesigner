using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;

namespace Sawczyn.EFDesigner.EFModel
{
   /// <summary>
   /// Used to define compartment shapes that have child elements that can be dragged up and down to reorder them (ClassShape and EnumShape)
   /// </summary>
   public interface IHasDraggableChildren
   {
      Diagram Diagram { get; }

      void DoMouseUp(ModelElement dragFrom, DiagramMouseEventArgs e);
   }
}
