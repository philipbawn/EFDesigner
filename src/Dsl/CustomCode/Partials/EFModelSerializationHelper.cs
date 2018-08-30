using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Xml;

using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Validation;

namespace Sawczyn.EFDesigner.EFModel
{
   public partial class EFModelSerializationHelper
   {
      private const int MIN_FILE_LENGTH = 5;

      /// <summary>
      ///    Checks the version of the file being read.
      /// </summary>
      /// <param name="serializationContext">Serialization context.</param>
      /// <param name="reader">
      ///    Reader for the file being read. The reader is positioned at the open tag of the root element being
      ///    read.
      /// </param>
      protected override void CheckVersion(SerializationContext serializationContext, XmlReader reader)
      {
         #region Check Parameters

         Debug.Assert(serializationContext != null);

         if (serializationContext == null)
            throw new ArgumentNullException(nameof(serializationContext));

         Debug.Assert(reader != null);

         if (reader == null)
            throw new ArgumentNullException(nameof(reader));

         #endregion

         string dslVersionStr = reader.GetAttribute("dslVersion");

         if (dslVersionStr != null)
         {
            try
            {
               Version actualVersion = new Version(dslVersionStr);

               if (actualVersion.Major != 1)
                  EFModelSerializationBehaviorSerializationMessages.VersionMismatch(serializationContext, reader, new Version(1, 0), actualVersion);
            }
            catch (ArgumentException)
            {
               EFModelSerializationBehaviorSerializationMessages.InvalidPropertyValue(serializationContext, reader, "dslVersion", typeof(Version), dslVersionStr);
            }
            catch (FormatException)
            {
               EFModelSerializationBehaviorSerializationMessages.InvalidPropertyValue(serializationContext, reader, "dslVersion", typeof(Version), dslVersionStr);
            }
            catch (OverflowException)
            {
               EFModelSerializationBehaviorSerializationMessages.InvalidPropertyValue(serializationContext, reader, "dslVersion", typeof(Version), dslVersionStr);
            }
         }
      }

      private EFModelDiagram LoadDiagram(SerializationResult serializationResult, ModelElement modelRoot, Stream diagramStream, ISchemaResolver schemaResolver, Partition diagramPartition, TransactionContext transactionContext, SerializationContext serializationContext, DomainClassXmlSerializer diagramSerializer)
      {
         EFModelDiagram result = null;

         using (Transaction t = diagramPartition.Store.TransactionManager.BeginTransaction("LoadDiagram", true, transactionContext))
         {
            // Ensure there is some content in the file. Blank (or almost blank, to account for encoding header bytes, etc.)
            // files will cause a new diagram to be created and returned 
            if (diagramStream.Length > MIN_FILE_LENGTH)
            {
               XmlReaderSettings settings = CreateXmlReaderSettings(serializationContext, false);

               try
               {
                  using (XmlReader reader = XmlReader.Create(diagramStream, settings))
                  {
                     reader.MoveToContent();
                     result = diagramSerializer.TryCreateInstance(serializationContext, reader, diagramPartition) as EFModelDiagram;

                     if (result != null)
                        ReadRootElement(serializationContext, result, reader, schemaResolver);
                  }
               }
               catch (XmlException xEx)
               {
                  SerializationUtilities.AddMessage(serializationContext, SerializationMessageKind.Error, xEx);
               }

               if (serializationResult.Failed)
               {
                  // Serialization error encountered, rollback the transaction.
                  result = null;
                  t.Rollback();
               }
            }

            // Create diagram if it doesn't exist
            if (result == null && !serializationResult.Failed)
               result = CreateDiagramHelper(diagramPartition, modelRoot);

            if (t.IsActive) 
               t.Commit();
         } // End inner Tx

         return result;
      }

      public override ModelRoot LoadModelAndDiagram(SerializationResult serializationResult, Partition modelPartition, string modelFileName, Partition diagramPartition, string diagramFileName, ISchemaResolver schemaResolver, ValidationController validationController, ISerializerLocator serializerLocator)
      {
         #region Check Parameters

         if (modelPartition == null)
            throw new ArgumentNullException(nameof(modelPartition));
         if (diagramPartition == null)
            throw new ArgumentNullException(nameof(diagramPartition));
         if (modelFileName == null)
            throw new ArgumentNullException(nameof(modelFileName));
         if (diagramFileName == null)
            throw new ArgumentNullException(nameof(diagramFileName));
         #endregion

         // before loading anything, let's check to see if the diagram is old-school or our new compressed version
         // if it's the new one, we don't want to load anything and pollute the Store
         // if anyone knows of a better way of doing this than catching an exception, please post a comment on Github! :-)

         try
         {
            using (Package _ = Package.Open(diagramFileName, FileMode.Open, FileAccess.Read))
            {

            }
         }
         catch (FileFormatException)
         {
            // old-style xml diagram file; not a zip. use old school processing
            return base.LoadModelAndDiagram(serializationResult, modelPartition, modelFileName, diagramPartition, diagramFileName, schemaResolver, validationController, serializerLocator);
         }

         // Load the model
         ModelRoot modelRoot = LoadModel(serializationResult, modelPartition.Store, modelFileName, schemaResolver, validationController, null);

         // don't try to deserialize diagram data if model load failed.
         if (serializationResult.Failed)
            return modelRoot;

         FileInfo fileInfo = new FileInfo(diagramFileName);

         if (!fileInfo.Exists || fileInfo.Length <= MIN_FILE_LENGTH)
         {
            using (Transaction transaction = diagramPartition.Store.TransactionManager.BeginTransaction("New diagram"))
            {
               EFModelDiagram newDiagram = CreateDiagramHelper(modelPartition, modelRoot);
               newDiagram.ModelElement = modelRoot;
               OnPostLoadModelAndDiagram(serializationResult, modelPartition, modelFileName, diagramPartition, diagramFileName, modelRoot, new[] { newDiagram });
               transaction.Commit();
            }
         }
         else
         {
            try
            {
               using (Package pkgOutputDoc = Package.Open(diagramFileName, FileMode.Open, FileAccess.Read))
               {
                  DomainXmlSerializerDirectory directory = GetDirectory(diagramPartition.Store);
                  DomainClassXmlSerializer diagramSerializer = directory.GetSerializer(EFModelDiagram.DomainClassId);

                  if (diagramSerializer != null)
                  {
                     SerializationContext serializationContext = new SerializationContext(directory, "LoadDiagram", serializationResult);
                     InitializeSerializationContext(diagramPartition, serializationContext, true);

                     TransactionContext transactionContext = new TransactionContext();
                     transactionContext.Add(SerializationContext.TransactionContextKey, serializationContext);

                     using (Transaction postT = diagramPartition.Store.TransactionManager.BeginTransaction("PostLoad Model and Diagram", true, transactionContext))
                     {
                        List<EFModelDiagram> diagrams = new List<EFModelDiagram>();

                        foreach (PackagePart packagePart in pkgOutputDoc.GetParts())
                        {
                           Stream diagramStream = packagePart.GetStream(FileMode.Open, FileAccess.Read);
                           EFModelDiagram diagram = LoadDiagram(serializationResult, modelRoot, diagramStream, schemaResolver, diagramPartition, transactionContext, serializationContext, diagramSerializer);

                           if (diagram != null)
                           {
                              if (!serializationResult.Failed)
                              {
                                 // Succeeded.
                                 diagram.ModelElement = modelRoot;
                                 diagrams.Add(diagram);
                                 diagram.PostDeserialization(true);
                                 CheckForOrphanedShapes(diagram, serializationResult);
                              }
                              else
                              {
                                 diagram.PostDeserialization(false);
                              }
                           }
                        }

                        OnPostLoadModelAndDiagram(serializationResult, modelPartition, modelFileName, diagramPartition, diagramFileName, modelRoot, diagrams);

                        // Do load-time validation if a ValidationController is provided.
                        if (!serializationResult.Failed && validationController != null)
                        {
                           using (new SerializationValidationObserver(serializationResult, validationController))
                           {
                              validationController.Validate(diagramPartition, ValidationCategories.Load);
                           }
                        }

                        if (serializationResult.Failed)
                        {
                           // Serialization error encountered, rollback the middle transaction.
                           modelRoot = null;
                           postT.Rollback();
                        }

                        if (postT.IsActive)
                           postT.Commit();
                     }
                  }
               }
            }
            catch (FileFormatException)
            {
               // old-style xml diagram file; not a zip. use old school processing
               modelRoot = base.LoadModelAndDiagram(serializationResult, modelPartition, modelFileName, diagramPartition, diagramFileName, schemaResolver, validationController, serializerLocator);
            }
         }

         return modelRoot;
      }

      internal void SaveDiagrams(SerializationResult serializationResult, EFModelDiagram[] diagrams, string diagramFileName, Encoding encoding, bool writeOptionalPropertiesWithDefaultValue)
      {
         #region Check Parameters

         if (serializationResult == null)
            throw new ArgumentNullException(nameof(serializationResult));
         if (string.IsNullOrEmpty(diagramFileName))
            throw new ArgumentNullException(nameof(diagramFileName));
         if (diagrams.Count(x => string.IsNullOrEmpty(x.Name)) > 1)
            throw new ArgumentException("File can have only one unnamed (default) diagram", nameof(diagrams));

         #endregion

         Dictionary<MemoryStream, string> memoryStreamDictionary = new Dictionary<MemoryStream, string>();

         foreach (EFModelDiagram diagram in diagrams)
         {
            MemoryStream stream = InternalSaveDiagram(serializationResult, diagram, diagramFileName, encoding, writeOptionalPropertiesWithDefaultValue);
            memoryStreamDictionary.Add(stream, diagram.Name);
            if (serializationResult.Failed)
            {
               foreach (MemoryStream memoryStream in memoryStreamDictionary.Keys.ToList())
                  memoryStream.Close();

               return;
            }
         }

         using (Package pkgOutputDoc = Package.Open(diagramFileName, FileMode.Create, FileAccess.ReadWrite))
         {
            foreach (MemoryStream memoryStream in memoryStreamDictionary.Keys)
            {
               byte[] bytes = memoryStream.ToArray();
               Uri uri = new Uri($"/diagrams/{memoryStreamDictionary[memoryStream]}.diagram", UriKind.Relative);
               PackagePart part = pkgOutputDoc.CreatePart(uri, MediaTypeNames.Text.Xml, CompressionOption.Maximum);
               using (Stream partStream = part.GetStream(FileMode.Create, FileAccess.Write))
               {
                  partStream.Write(bytes, 0, bytes.Length);
               }
            }
         }
      }

      public void SaveModelAndDiagram(SerializationResult serializationResult, ModelRoot modelRoot, string modelFileName, IEnumerable<EFModelDiagram> diagrams, string diagramFileName, Encoding encoding, bool writeOptionalPropertiesWithDefaultValue)
      {
         List<EFModelDiagram> diagramList = diagrams.ToList();

         #region Check Parameters

         if (serializationResult == null)
            throw new ArgumentNullException(nameof(serializationResult));

         if (string.IsNullOrEmpty(modelFileName))
            throw new ArgumentNullException(nameof(modelFileName));

         if (string.IsNullOrEmpty(diagramFileName))
            throw new ArgumentNullException(nameof(diagrams));

         if (diagramList.Count(x => string.IsNullOrEmpty(x.Name)) > 1)
            throw new ArgumentException("File can have only one unnamed (default) diagram", nameof(diagrams));

         #endregion

         if (serializationResult.Failed)
            return;

         MemoryStream modelFileContent = InternalSaveModel(serializationResult, modelRoot, modelFileName, encoding, writeOptionalPropertiesWithDefaultValue);

         if (serializationResult.Failed)
         {
            modelFileContent.Close();
            return;
         }

         Dictionary<MemoryStream, string> memoryStreamDictionary = new Dictionary<MemoryStream, string>();

         foreach (EFModelDiagram diagram in diagramList)
         {
            MemoryStream memoryStream = InternalSaveDiagram(serializationResult, diagram, diagramFileName, encoding, writeOptionalPropertiesWithDefaultValue);
            memoryStreamDictionary.Add(memoryStream, diagram.Name);

            if (serializationResult.Failed)
            {
               modelFileContent.Close();

               foreach (MemoryStream stream in memoryStreamDictionary.Keys)
                  stream.Close();

               return;
            }
         }

         if (modelFileContent != null)
         {
            using (FileStream fileStream = new FileStream(modelFileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
               using (BinaryWriter writer = new BinaryWriter(fileStream, encoding))
               {
                  writer.Write(modelFileContent.ToArray());
               }
            }

            modelFileContent.Close();
         }

         using (Package pkgOutputDoc = Package.Open(diagramFileName, FileMode.Create, FileAccess.ReadWrite))
         {
            foreach (MemoryStream memoryStream in memoryStreamDictionary.Keys)
            {
               byte[] bytes = memoryStream.ToArray();
               Uri uri = new Uri($"/diagrams/{memoryStreamDictionary[memoryStream]}", UriKind.Relative);
               PackagePart part = pkgOutputDoc.CreatePart(uri, MediaTypeNames.Text.Xml, CompressionOption.Maximum);

               using (Stream partStream = part.GetStream(FileMode.Create, FileAccess.Write))
               {
                  partStream.Write(bytes, 0, bytes.Length);
               }
            }
         }
      }
   }
}
