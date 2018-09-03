using System;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.Modeling;

namespace Sawczyn.EFDesigner.EFModel
{
   internal partial class EFModelExplorerToolWindow
   {
      protected override void OnSelectionChanged(EventArgs e)
      {
         base.OnSelectionChanged(e);

         // select element on active diagram
         if (PrimarySelection != null)
         {
            if (PrimarySelection is ModelElement modelElement)
            {
               modelElement.LocateInDiagram(true);
            }
            else if (PrimarySelection is EFModelDiagramProxy diagramProxy)
            {
               EFModelDocData.Current.OpenView(EFModelDiagram.DomainClassId, diagramProxy.Name);
            }
         }
      }
   }
}
