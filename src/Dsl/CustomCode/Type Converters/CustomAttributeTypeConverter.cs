using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sawczyn.EFDesigner.EFModel
{
   public class CustomAttributeTypeConverter : TypeConverterBase
   {
      /// <summary>
      ///    Returns whether the collection of standard values returned from
      ///    <see cref="M:System.ComponentModel.TypeConverter.GetStandardValues" /> is an exclusive list of possible values,
      ///    using the specified context.
      /// </summary>
      /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context. </param>
      /// <returns>
      ///    true if the <see cref="T:System.ComponentModel.TypeConverter.StandardValuesCollection" /> returned from
      ///    <see cref="M:System.ComponentModel.TypeConverter.GetStandardValues" /> is an exhaustive list of possible values;
      ///    false if other values are possible.
      /// </returns>
      public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
      {
         return false;
      }

      /// <summary>
      ///    Returns whether this object supports a standard set of values that can be picked from a list, using the
      ///    specified context.
      /// </summary>
      /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context. </param>
      /// <returns>
      ///    true if <see cref="M:System.ComponentModel.TypeConverter.GetStandardValues" /> should be called to find a
      ///    common set of values the object supports; otherwise, false.
      /// </returns>
      public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
      {
         return false;
      }
   }

   public class Attributes : List<Attribute> { }

   public class Attribute
   {
      public string Name { get; set; }
      public List<AttributeParameter> Parameters { get; set; }
   }

   public class AttributeParameter
   {
      public string Name { get; set; }
   }
}
