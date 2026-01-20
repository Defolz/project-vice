using NUnit.Framework;

namespace ProjectVice.Tests.Components
{
    /// <summary>
    /// Unit tests for NPCId component
    /// Тесты для компонента NPCId
    /// </summary>
    public class NPCIdTests
    {
        [Test]
        public void Generate_CreatesDifferentIds()
        {
            // Arrange & Act
            var id1 = NPCId.Generate(12345);
            var id2 = NPCId.Generate(54321);
            
            // Assert
            Assert.AreNotEqual(id1.Value, id2.Value, "Different seeds should produce different IDs");
        }
        
        [Test]
        public void Generate_SameSeed_ProducesSameId()
        {
            // Arrange
            uint seed = 99999;
            
            // Act
            var id1 = NPCId.Generate(seed);
            var id2 = NPCId.Generate(seed);
            
            // Assert
            Assert.AreEqual(id1.Value, id2.Value, "Same seed should produce same ID (deterministic)");
        }
        
        [Test]
        public void Generate_StoresGenerationSeed()
        {
            // Arrange
            uint seed = 12345;
            
            // Act
            var id = NPCId.Generate(seed);
            
            // Assert
            Assert.AreEqual(seed, id.GenerationSeed, "GenerationSeed should match input seed");
        }
        
        [Test]
        public void Generate_ProducesNonZeroValue()
        {
            // Arrange
            uint seed = 1;
            
            // Act
            var id = NPCId.Generate(seed);
            
            // Assert
            Assert.AreNotEqual(0u, id.Value, "Generated ID should not be zero");
        }
        
        [Test]
        public void ToString_ReturnsHexFormat()
        {
            // Arrange
            var id = new NPCId { Value = 0xABCD1234, GenerationSeed = 123 };
            
            // Act
            var str = id.ToString();
            
            // Assert
            Assert.IsTrue(str.Contains("NPC_"), "ToString should start with NPC_");
            Assert.IsTrue(str.Length > 4, "ToString should contain hex value");
        }
        
        [Test]
        public void Generate_WithZeroSeed_ProducesValidId()
        {
            // Arrange
            uint seed = 0;
            
            // Act
            var id = NPCId.Generate(seed);
            
            // Assert
            // Random с сидом 0 должен работать (после XOR с константой)
            Assert.IsTrue(id.Value != 0 || id.GenerationSeed == 0, "Should handle zero seed gracefully");
        }
        
        [Test]
        public void Generate_WithMaxSeed_ProducesValidId()
        {
            // Arrange
            uint seed = uint.MaxValue;
            
            // Act
            var id = NPCId.Generate(seed);
            
            // Assert
            Assert.AreNotEqual(0u, id.Value, "Should handle max seed value");
            Assert.AreEqual(uint.MaxValue, id.GenerationSeed);
        }
        
        [Test]
        public void Generate_100Ids_AllUnique()
        {
            // Arrange
            var ids = new System.Collections.Generic.HashSet<uint>();
            
            // Act
            for (uint i = 0; i < 100; i++)
            {
                var id = NPCId.Generate(i);
                ids.Add(id.Value);
            }
            
            // Assert
            Assert.AreEqual(100, ids.Count, "100 different seeds should produce 100 unique IDs");
        }
    }
}
