﻿using System;
using System.IO;
using System.Threading;

using KenticoInspector.Core.Models;
using KenticoInspector.Core.Services.Interfaces;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KenticoInspector.Core.Helpers
{
    public class ReportMetadataService : IReportMetadataService
    {
        public string DefaultCultureName => "en-US";

        public string CurrentCultureName => Thread.CurrentThread.CurrentCulture.Name;

        public ReportMetadata<T> GetReportMetadata<T>(string reportCodename) where T : new()
        {
            var metadataDirectory = $"{DirectoryHelper.GetExecutingDirectory()}\\{reportCodename}\\Metadata\\";

            var defaultCultureMetadataPath = $"{metadataDirectory}{DefaultCultureName}.yaml";

            var currentCultureMetadataPath = $"{metadataDirectory}{CurrentCultureName}.yaml";

            var currentCultureIsDefaultCulture = DefaultCultureName == CurrentCultureName;

            var currentCultureMetadataPathExists = File.Exists(currentCultureMetadataPath);

            if (!currentCultureIsDefaultCulture && currentCultureMetadataPathExists)
            {
                var defaultCultureMetadata = DeserializeYaml<ReportMetadata<T>>(defaultCultureMetadataPath);

                var currentCultureMetadata = DeserializeYaml<ReportMetadata<T>>(currentCultureMetadataPath, true);

                return GetMergedMetadata(defaultCultureMetadata, currentCultureMetadata);
            }

            return DeserializeYaml<ReportMetadata<T>>(defaultCultureMetadataPath);
        }

        private T DeserializeYaml<T>(string path, bool ignoreUnmatchedProperties = false)
        {
            var deserializerBuilder = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention());

            if (ignoreUnmatchedProperties)
            {
                deserializerBuilder
                    .IgnoreUnmatchedProperties();
            }

            var deserializer = deserializerBuilder
                .Build();

            var yamlFile = File.ReadAllText(path);

            return deserializer.Deserialize<T>(yamlFile);
        }

        private ReportMetadata<T> GetMergedMetadata<T>(ReportMetadata<T> defaultMetadata, ReportMetadata<T> overrideMetadata) where T : new()
        {
            var mergedMetadata = new ReportMetadata<T>
            {
                Details = new ReportDetails(),
                Terms = new T()
            };

            mergedMetadata.Details.Name = overrideMetadata.Details?.Name ?? defaultMetadata.Details.Name;
            mergedMetadata.Details.ShortDescription = overrideMetadata.Details?.ShortDescription ?? defaultMetadata.Details.ShortDescription;
            mergedMetadata.Details.LongDescription = overrideMetadata.Details?.LongDescription ?? defaultMetadata.Details.LongDescription;

            RecursivelySetPropertyValues(typeof(T), defaultMetadata.Terms, overrideMetadata.Terms, mergedMetadata.Terms);

            return mergedMetadata;
        }

        private static void RecursivelySetPropertyValues(Type objectType, object defaultObject, object overrideObject, object targetObject)
        {
            var objectTypeProperties = objectType.GetProperties();

            foreach (var objectTypeProperty in objectTypeProperties)
            {
                var objectTypePropertyType = objectTypeProperty.PropertyType;

                var defaultObjectPropertyValue = objectTypeProperty.GetValue(defaultObject);

                object overrideObjectPropertyValue = null;

                if (overrideObject != null)
                {
                    overrideObjectPropertyValue = objectTypeProperty.GetValue(overrideObject);
                }

                if (objectTypePropertyType.Namespace == objectType.Namespace)
                {
                    var targetObjectPropertyValue = Activator.CreateInstance(objectTypePropertyType);

                    objectTypeProperty.SetValue(targetObject, targetObjectPropertyValue);

                    RecursivelySetPropertyValues(objectTypePropertyType, defaultObjectPropertyValue, overrideObjectPropertyValue, targetObjectPropertyValue);
                }
                else
                {
                    objectTypeProperty.SetValue(targetObject, overrideObjectPropertyValue ?? defaultObjectPropertyValue);
                }
            }
        }
    }
}