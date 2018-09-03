using System.Linq;

namespace Sawczyn.EFDesigner.EFModel
{
   public partial class EFModelDiagramProxy
   {
      public EFModelDiagram AssociatedDiagram => Store.DefaultPartitionForClass(EFModelDiagram.DomainClassId)
                                                      .ElementDirectory
                                                      .AllElements
                                                      .OfType<EFModelDiagram>()
                                                      .FirstOrDefault(d => d.Name == Name ||
                                                                           (d.Name == null && Name == "Default"));
   }
}
