using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Modeling.Shell;

namespace Sawczyn.EFDesigner.EFModel
{
   internal partial class EFModelDocView
   {
      private readonly string physicalView;

      public EFModelDocView(ModelingDocData docData
                          , IServiceProvider serviceProvider
                          , string physicalView)
          : base(docData, serviceProvider)
      {
         this.physicalView = physicalView;
      }

      protected override bool LoadView()
      {
         if (DocData.RootElement != null)
         {
            List<Diagram> diagramList = DocData.Store.ElementDirectory.FindElements<Diagram>().ToList();

            if (diagramList.Any())
            {
               Diagram = (string.IsNullOrEmpty(physicalView) || physicalView == "Default"
                             ? diagramList[0]
                             : diagramList.Find(d => d.Name == physicalView)) 
                      ?? diagramList[0];

               return BaseLoadView();
            }
         }

         return false;

      }
   }
}
