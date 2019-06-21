using KenticoInspector.Core.Constants;
using KenticoInspector.Core.Models;
using KenticoInspector.Core.Services.Interfaces;
using KenticoInspector.Reports.ClassTableValidation;
using KenticoInspector.Reports.Tests.Helpers;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Dynamic;

namespace KenticoInspector.Reports.Tests
{
    [TestFixture(10)]
    [TestFixture(11)]
    [TestFixture(12)]
    public class ClassTableValidationTests
    {
        private Mock<IDatabaseService> _mockDatabaseService;
        private Instance _mockInstance;
        private InstanceDetails _mockInstanceDetails;
        private Mock<IInstanceService> _mockInstanceService;
        private Report _mockReport;

        public ClassTableValidationTests(int majorVersion)
        {
            InitializeCommonMocks(majorVersion);
            _mockReport = new Report(_mockDatabaseService.Object, _mockInstanceService.Object);
        }

        [Test]
        public void Should_ReturnCleanResult_When_DatabaseIsClean()
        {
            // Arrange
            var tableResults = GetCleanTableResults();
            _mockDatabaseService
                .Setup(p => p.ExecuteSqlFromFile<TableWithNoClass>(Scripts.TablesWithNoClass))
                .Returns(tableResults);

            var classResults = GetCleanClassResults();
            _mockDatabaseService
                .Setup(p => p.ExecuteSqlFromFile<ClassWithNoTable>(Scripts.ClassesWithNoTable))
                .Returns(classResults);

            // Act
            var results = _mockReport.GetResults(_mockInstance.Guid);

            // Assert
            Assert.That(results.Data.TableResults.Rows.Count == 0);
            Assert.That(results.Data.ClassResults.Rows.Count == 0);
            Assert.That(results.Status == ReportResultsStatus.Good);
        }

        [Test]
        public void Should_ReturnErrorResult_When_DatabaseHasClassWithNoTable()
        {
            // Arrange
            var tableResults = GetCleanTableResults();
            _mockDatabaseService
                .Setup(p => p.ExecuteSqlFromFile<TableWithNoClass>(Scripts.TablesWithNoClass))
                .Returns(tableResults);

            var classResults = GetCleanClassResults();
            classResults.Add(new ClassWithNoTable
            {
                ClassDisplayName = "Has no table",
                ClassName = "HasNoTable",
                ClassTableName = "Custom_HasNoTable"
            });

            _mockDatabaseService
                .Setup(p => p.ExecuteSqlFromFile<ClassWithNoTable>(Scripts.ClassesWithNoTable))
                .Returns(classResults);

            // Act
            var results = _mockReport.GetResults(_mockInstance.Guid);

            // Assert
            Assert.That(results.Data.TableResults.Rows.Count == 0);
            Assert.That(results.Data.ClassResults.Rows.Count == 1);
            Assert.That(results.Status == ReportResultsStatus.Error);
        }

        [Test]
        public void Should_ReturnErrorResult_When_DatabaseHasTableWithNoClass()
        {
            // Arrange
            var tableResults = GetCleanTableResults(false);
            tableResults.Add(new TableWithNoClass {
                TableName = "HasNoClass"
            });

            _mockDatabaseService
                .Setup(p => p.ExecuteSqlFromFile<TableWithNoClass>(Scripts.TablesWithNoClass))
                .Returns(tableResults);

            var classResults = GetCleanClassResults();
            _mockDatabaseService
                .Setup(p => p.ExecuteSqlFromFile<ClassWithNoTable>(Scripts.ClassesWithNoTable))
                .Returns(classResults);

            // Act
            var results = _mockReport.GetResults(_mockInstance.Guid);

            // Assert
            Assert.That(results.Data.TableResults.Rows.Count == 1);
            Assert.That(results.Data.ClassResults.Rows.Count == 0);
            Assert.That(results.Status == ReportResultsStatus.Error);
        }

        private List<ClassWithNoTable> GetCleanClassResults()
        {
            return new List<ClassWithNoTable>();
        }

        private List<TableWithNoClass> GetCleanTableResults(bool includeWhitelistedTables = true)
        {
            var tableResults = new List<TableWithNoClass>();
            if (includeWhitelistedTables && _mockInstanceDetails.DatabaseVersion.Major >= 10)
            {
                tableResults.Add(new TableWithNoClass() { TableName = "CI_Migration" });
            }

            return tableResults;
        }

        private void InitializeCommonMocks(int majorVersion)
        {
            _mockInstance = MockInstances.Get(majorVersion);
            _mockInstanceDetails = MockInstanceDetails.Get(majorVersion, _mockInstance);
            _mockInstanceService = MockInstanceServiceHelper.SetupInstanceService(_mockInstance, _mockInstanceDetails);
            _mockDatabaseService = MockDatabaseServiceHelper.SetupMockDatabaseService(_mockInstance);
        }
    }
}