using NUnit.Framework;

namespace ProjectVice.Tests.Components
{
    /// <summary>
    /// Unit tests for Faction component
    /// Тесты для компонента Faction
    /// </summary>
    public class FactionTests
    {
        [Test]
        public void Constructor_WithEnum_SetsCorrectType()
        {
            // Arrange & Act
            var faction = new Faction(FactionType.Families);
            
            // Assert
            Assert.AreEqual(FactionType.Families, faction.Type);
            Assert.AreEqual(1, faction.Value);
        }
        
        [Test]
        public void Constructor_WithInt_SetsCorrectType()
        {
            // Arrange & Act
            var faction = new Faction(2);
            
            // Assert
            Assert.AreEqual(FactionType.Colombians, faction.Type);
            Assert.AreEqual(2, faction.Value);
        }
        
        [Test]
        public void StaticFactions_HaveCorrectValues()
        {
            // Assert
            Assert.AreEqual(FactionType.Invalid, Faction.Invalid.Type);
            Assert.AreEqual(FactionType.Families, Faction.Families.Type);
            Assert.AreEqual(FactionType.Colombians, Faction.Colombians.Type);
            Assert.AreEqual(FactionType.FBI, Faction.FBI.Type);
            Assert.AreEqual(FactionType.Police, Faction.Police.Type);
            Assert.AreEqual(FactionType.Civilians, Faction.Civilians.Type);
        }
        
        [Test]
        public void IsValid_InvalidFaction_ReturnsFalse()
        {
            // Arrange
            var faction = Faction.Invalid;
            
            // Act & Assert
            Assert.IsFalse(faction.IsValid, "Invalid faction should return false");
        }
        
        [Test]
        public void IsValid_ValidFaction_ReturnsTrue()
        {
            // Arrange
            var faction = Faction.Families;
            
            // Act & Assert
            Assert.IsTrue(faction.IsValid, "Valid faction should return true");
        }
        
        [Test]
        public void ToString_AllFactions_ReturnCorrectNames()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("Families", Faction.Families.ToString());
            Assert.AreEqual("Colombians", Faction.Colombians.ToString());
            Assert.AreEqual("FBI", Faction.FBI.ToString());
            Assert.AreEqual("Police", Faction.Police.ToString());
            Assert.AreEqual("Civilians", Faction.Civilians.ToString());
        }
        
        [Test]
        public void ToString_InvalidFaction_ReturnsUnknown()
        {
            // Arrange
            // Use a value within byte range (0-255) but outside valid factions (0-5)
            var faction = new Faction(200);
            
            // Act
            var str = faction.ToString();
            
            // Assert
            Assert.IsTrue(str.Contains("Unknown"), "Unknown faction should contain 'Unknown' in string");
            Assert.IsTrue(str.Contains("200"), "Unknown faction should contain its value");
        }
        
        [Test]
        public void Value_Property_ReturnsIntValue()
        {
            // Arrange
            var faction = new Faction(FactionType.FBI);
            
            // Act
            var value = faction.Value;
            
            // Assert
            Assert.AreEqual(3, value, "FBI faction should have value 3");
        }
        
        [Test]
        public void AllFactionTypes_HaveUniqueValues()
        {
            // Arrange
            var values = new System.Collections.Generic.HashSet<int>();
            
            // Act
            values.Add(Faction.Invalid.Value);
            values.Add(Faction.Families.Value);
            values.Add(Faction.Colombians.Value);
            values.Add(Faction.FBI.Value);
            values.Add(Faction.Police.Value);
            values.Add(Faction.Civilians.Value);
            
            // Assert
            Assert.AreEqual(6, values.Count, "All factions should have unique values");
        }
    }
}
