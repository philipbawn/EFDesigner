using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Modeling;

namespace Sawczyn.EFDesigner.EFModel
{
   public static class ModelRootExtensions
   {
      public static ICollection<ModelElement> AllElements(this ModelRoot modelRoot)
      {
         return modelRoot.Store.ElementDirectory.AllElements;
      }

      public static Transaction BeginTransaction(this ModelRoot modelRoot, string tag)
      {
         return modelRoot.Store.TransactionManager.BeginTransaction(tag);
      }
   }
}
