using System;
using System.Collections.Generic;
using System.Linq;

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
            BaseLoadView();

            if (DocData.RootElement != null)
            {
               List<Diagram> diagramList = DocData.Store.ElementDirectory.FindElements<Diagram>().ToList();

                if (diagramList.Any())
                {
                    Diagram diagram = string.IsNullOrEmpty(physicalView) || physicalView == "Default"
                                          ? diagramList[0]
                                          : diagramList.Find(d => d.Name == physicalView);

                    return (Diagram = diagram) != null;
                }
            }

            return false;

        }
    }
}
