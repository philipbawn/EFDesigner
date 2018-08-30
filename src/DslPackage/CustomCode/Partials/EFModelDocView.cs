using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;

namespace Sawczyn.EFDesigner.EFModel
{
   internal partial class EFModelDocView
   {
      protected EFModelExplorerToolWindow ModelExplorerWindow => EFModelPackage.Instance?.GetToolWindow(typeof(EFModelExplorerToolWindow), true) as EFModelExplorerToolWindow;
      private string physicalView;

      public EFModelDocView(EFModelDocData docData, IServiceProvider serviceProvider, string physicalView) : base(docData, serviceProvider)
      {
         this.physicalView = physicalView;
      }

      protected override bool LoadView()
      {
         BaseLoadView();

         Debug.Assert(DocData.RootElement != null);

         if (DocData.RootElement == null)
            return false;

         List<EFModelDiagram> diagrams = DocData.Store.ElementDirectory.AllElements.OfType<EFModelDiagram>().ToList();

         if (!diagrams.Any())
            return false;

         Diagram = string.IsNullOrEmpty(physicalView)
                                     ? diagrams[0]
                                     : diagrams.FirstOrDefault(d => d.Name == physicalView);

         return (Diagram != null);
      }

      /// <summary>
      ///    Called when selection changes in this window.
      /// </summary>
      /// <remarks>
      ///    Overriden to update the F1 help keyword for the selection.
      /// </remarks>
      /// <param name="e"></param>
      protected override void OnSelectionChanged(EventArgs e)
      {
         base.OnSelectionChanged(e);

         // TODO: look into how we can reset the explorer's selected node when the selection changes on the diagram without causing a recursive call that messes up the view of the diagram.

         //List<ModelElement> selected_diagram = SelectedElements.OfType<ModelElement>().ToList();
         //List<ModelElement> selected_explorer = ModelExplorerWindow?.GetSelectedComponents()?.OfType<ModelElement>() != null
         //                                          ? ModelExplorerWindow.GetSelectedComponents().OfType<ModelElement>().ToList()
         //                                          : null;

         //if (selected_explorer != null)
         //{
         //   if (selected_diagram.Count != 1)
         //      ModelExplorerWindow.SetSelectedComponents(null);
         //   else if (selected_diagram[0] != selected_explorer.FirstOrDefault())
         //      ModelExplorerWindow.SetSelectedComponents(selected_diagram);
         //}
      }
   }
}
