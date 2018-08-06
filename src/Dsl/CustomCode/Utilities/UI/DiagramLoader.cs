using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sawczyn.EFDesigner.EFModel
{
   public static class DiagramLoader
   {
      private static Action<string> DiagramLoadMethod { get; set; }

      public static void RegisterLoadMethod(Action<string> loadMethod)
      {
         DiagramLoadMethod = loadMethod;
      }

      public static void LoadDiagram(string diagramName)
      {
         DiagramLoadMethod?.Invoke(diagramName);
      }
   }
}
