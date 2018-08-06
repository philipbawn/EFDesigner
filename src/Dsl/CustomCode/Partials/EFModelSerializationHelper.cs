using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Modeling.Validation;

namespace Sawczyn.EFDesigner.EFModel
{
   partial class EFModelSerializationHelper
   {
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
            catch (Exception e) when (e is ArgumentException || e is FormatException || e is OverflowException)
            {
               EFModelSerializationBehaviorSerializationMessages.InvalidPropertyValue(serializationContext, reader, "dslVersion", typeof(Version), dslVersionStr);
            }
         }
      }

      private MemoryStream InternalSaveModel(SerializationResult serializationResult, ModelElement modelRoot, string fileName, Encoding encoding, bool writeOptionalPropertiesWithDefaultValue)
      {
         #region Check Parameters
         Debug.Assert(serializationResult != null);
         Debug.Assert(modelRoot != null);
         Debug.Assert(!serializationResult.Failed);
         #endregion

         MemoryStream newFileContent = new MemoryStream();

         DomainXmlSerializerDirectory directory = GetDirectory(modelRoot.Store);
         DomainClassXmlSerializer modelRootSerializer = directory.GetSerializer(modelRoot.GetDomainClass().Id);

         if (modelRootSerializer != null)
         {
            SerializationContext serializationContext = new SerializationContext(directory, fileName, serializationResult)
            {
               WriteOptionalPropertiesWithDefaultValue = writeOptionalPropertiesWithDefaultValue
            };

            // MonikerResolver shouldn't be required in Save operation, so not calling SetupMonikerResolver() here.

            XmlWriterSettings settings = new XmlWriterSettings
            {
               Indent = true
                                          , Encoding = encoding
            };

            using (StreamWriter streamWriter = new StreamWriter(newFileContent, encoding))
            {
               using (XmlWriter writer = XmlWriter.Create(streamWriter, settings))
               {
                  modelRootSerializer.WriteRootElement(serializationContext, modelRoot, writer);
               }
            }
         }
         return newFileContent;
      }

      private MemoryStream InternalSaveDiagram(SerializationResult serializationResult, Diagram diagram, string diagramFileName, Encoding encoding, bool writeOptionalPropertiesWithDefaultValue)
      {
         #region Check Parameters

         Debug.Assert(serializationResult != null);
         Debug.Assert(diagram != null);
         Debug.Assert(!serializationResult.Failed);

         #endregion

         MemoryStream newFileContent = new MemoryStream();
         DomainXmlSerializerDirectory directory = GetDirectory(diagram.Store);
         DomainClassXmlSerializer diagramSerializer = directory.GetSerializer(diagram.GetDomainClass().Id)
                                                   ?? directory.GetSerializer(diagram.GetDomainClass().BaseDomainClass.Id);

         if (diagramSerializer != null)
         {
            SerializationContext serializationContext = new SerializationContext(directory, diagramFileName, serializationResult)
            {
               WriteOptionalPropertiesWithDefaultValue = writeOptionalPropertiesWithDefaultValue
            };

            // MonikerResolver shouldn't be required in Save operation, so not calling SetupMonikerResolver() here.

            XmlWriterSettings settings = new XmlWriterSettings
            {
               Indent = true
                                          , Encoding = encoding
            };

            using (StreamWriter streamWriter = new StreamWriter(newFileContent, encoding))
            {
               using (XmlWriter writer = XmlWriter.Create(streamWriter, settings))
               {
                  diagramSerializer.WriteRootElement(serializationContext, diagram, writer);
               }
            }
         }
         return newFileContent;
      }

      // ReSharper disable once UnusedMethodReturnValue.Local
      private Diagram LoadDiagram(SerializationResult serializationResult, Store store, ModelElement modelRoot, Stream diagramStream, ISchemaResolver schemaResolver, ValidationController validationController)
      {
         Diagram diagram = null;
         DomainXmlSerializerDirectory directory = GetDirectory(store);
         DomainClassXmlSerializer diagramSerializer = directory.GetSerializer(EFModelDiagram.DomainClassId);

         if (diagramSerializer != null)
         {
            if (diagramStream == Stream.Null || diagramStream == null || !diagramStream.CanRead)
            {
               // missing diagram file indicates we should create a new diagram.
               diagram = CreateDiagramHelper(store.DefaultPartition, modelRoot);
            }
            else
            {
               SerializationContext serializationContext = new SerializationContext(directory, "LoadDiagram", serializationResult);
               SetupMonikerResolver(serializationContext, store);

               using (Transaction transaction = store.TransactionManager.BeginTransaction("LoadDiagram", true))
               {
                  // Ensure there is some content in the file. Blank (or almost blank, to account for encoding header bytes, etc.)
                  // files will cause a new diagram to be created and returned 
                  if (diagramStream.Length > 5)
                  {
                     XmlReaderSettings settings = new XmlReaderSettings();
                     try
                     {
                        using (XmlReader reader = XmlReader.Create(diagramStream, settings))
                        {
                           reader.MoveToContent();
                           diagram = diagramSerializer.TryCreateInstance(serializationContext, reader, store.DefaultPartition) as EFModelDiagram;

                           if (diagram != null)
                              diagramSerializer.ReadRootElement(serializationContext, diagram, reader, schemaResolver);
                        }
                     }
                     catch (XmlException ex)
                     {
                        SerializationUtilities.AddMessage(serializationContext, SerializationMessageKind.Error, ex);
                     }

                     if (serializationResult.Failed)
                     {
                        // Serialization error encountered, rollback the transaction.
                        diagram = null;
                        transaction.Rollback();
                     }
                  }

                  if (diagram == null && !serializationResult.Failed)
                  {
                     // Create diagram if it doesn't exist
                     diagram = CreateDiagramHelper(store.DefaultPartition, modelRoot);
                  }

                  if (transaction.IsActive)
                     transaction.Commit();
               } // End inner Tx

               // Do load-time validation if a ValidationController is provided.
               if (!serializationResult.Failed && validationController != null)
               {
                  using (new SerializationValidationObserver(serializationResult, validationController))
                  {
                     validationController.Validate(store.DefaultPartition, ValidationCategories.Load);
                  }
               }
            }

            if (diagram != null)
            {
               if (!serializationResult.Failed)
               {
                  diagram.ModelElement = modelRoot;
                  diagram.PostDeserialization(true);
                  CheckForOrphanedShapes(diagram, serializationResult);
               }
               else
               {
                  diagram.PostDeserialization(false);
               }
            }
         }

         return diagram;
      }

      private void SetupMonikerResolver(SerializationContext lSerializationContext, Store store)
      {
         // Register the moniker resolver for this model, unless one is already registered
         IMonikerResolver monikerResolver = store.FindMonikerResolver(EFModelDomainModel.DomainModelId);
         if (monikerResolver == null)
         {

            monikerResolver = new EFModelSerializationBehaviorMonikerResolver(store, lSerializationContext.Directory);
            store.AddMonikerResolver(EFModelDomainModel.DomainModelId, monikerResolver);
         }
      }

      internal static class PackagingHelper
      {
         internal static bool IsValid(string fileName)
         {
            FileInfo fileInfo = new FileInfo(fileName);
            return fileInfo.Exists && (fileInfo.Length > 10);
         }
      }

      public ModelRoot LoadModelAndDiagrams(SerializationResult serializationResult, Store store, string modelFileName, string diagramxFileName, ISchemaResolver schemaResolver, ValidationController validationController, ISerializerLocator serializerLocator)
      {
         #region Check Parameters

         if (store == null)
            throw new ArgumentNullException(nameof(store));

         #endregion

         // Load the model
         ModelRoot modelRoot = LoadModel(store, modelFileName, schemaResolver, validationController, serializerLocator);

         if (serializationResult.Failed)
         {
            // don't try to deserialize diagram data if model load failed.
            return modelRoot;
         }

         if (PackagingHelper.IsValid(diagramxFileName))
         {
            using (Package pkgOutputDoc = Package.Open(diagramxFileName, FileMode.Open, FileAccess.Read))
            {
               foreach (PackagePart packagePart in pkgOutputDoc.GetParts())
                  LoadDiagram(serializationResult, store, modelRoot, packagePart.GetStream(FileMode.Open, FileAccess.Read), schemaResolver, validationController);
            }
         }
         else
            LoadDiagram(serializationResult, store, modelRoot, Stream.Null, schemaResolver, validationController);

         return modelRoot;
      }

      internal bool SaveDiagrams(SerializationResult serializationResult, Diagram[] diagrams, string diagramxFileName, Encoding encoding, bool writeOptionalPropertiesWithDefaultValue)
      {
         #region Check Parameters

         if (serializationResult == null)
            throw new ArgumentNullException(nameof(serializationResult));
         if (string.IsNullOrEmpty(diagramxFileName))
            throw new ArgumentNullException(nameof(diagrams));

         #endregion

         Dictionary<MemoryStream, string> memoryStreamDictionary = new Dictionary<MemoryStream, string>();

         foreach (Diagram diagram in diagrams)
         {
            memoryStreamDictionary.Add(InternalSaveDiagram(serializationResult, diagram, diagramxFileName, encoding, writeOptionalPropertiesWithDefaultValue), diagram.Name);

            if (serializationResult.Failed)
            {
               memoryStreamDictionary.Keys.ToList().ForEach(memoryStream => memoryStream.Close());
               return false;
            }
         }

         using (Package pkgOutputDoc = Package.Open(diagramxFileName, FileMode.Create, FileAccess.ReadWrite))
         {
            foreach (MemoryStream memoryStream in memoryStreamDictionary.Keys)
            {
               byte[] bytes = memoryStream.ToArray();
               Uri uri = new Uri($"/diagrams/{memoryStreamDictionary[memoryStream]}.diagram", UriKind.Relative);
               PackagePart part = pkgOutputDoc.CreatePart(uri, System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Maximum);

               using (Stream partStream = part.GetStream(FileMode.Create, FileAccess.Write))
               {
                  partStream.Write(bytes, 0, bytes.Length);
               }
            }
         }

         return true;
      }

      public void SaveModelAndDiagrams(SerializationResult serializationResult, ModelElement modelRoot, string modelFileName, Diagram[] diagrams, string diagramxFileName, Encoding encoding, bool writeOptionalPropertiesWithDefaultValue)
      {
         #region Check Parameters

         if (serializationResult == null)
            throw new ArgumentNullException(nameof(serializationResult));

         if (string.IsNullOrEmpty(modelFileName))
            throw new ArgumentNullException(nameof(modelFileName));

         if (string.IsNullOrEmpty(diagramxFileName))
            throw new ArgumentNullException(nameof(diagrams));

         #endregion

         if (serializationResult.Failed)
            return;

         MemoryStream modelFileContent = InternalSaveModel(serializationResult, modelRoot, modelFileName, encoding, writeOptionalPropertiesWithDefaultValue);

         if (serializationResult.Failed || !SaveDiagrams(serializationResult, diagrams, diagramxFileName, encoding, writeOptionalPropertiesWithDefaultValue))
         {
            modelFileContent?.Close();
            return;
         }

         using (FileStream fileStream = new FileStream(modelFileName, FileMode.Create, FileAccess.Write, FileShare.None))
         {
            using (BinaryWriter writer = new BinaryWriter(fileStream, encoding))
            {
               writer.Write(modelFileContent.ToArray());
            }
         }

         modelFileContent.Close();
      }
   }
}
