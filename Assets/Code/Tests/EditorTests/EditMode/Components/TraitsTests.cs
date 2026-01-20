using NUnit.Framework;

namespace ProjectVice.Tests.Components
{
    /// <summary>
    /// Unit tests for Traits component
    /// Тесты для компонента Traits
    /// </summary>
    public class TraitsTests
    {
        [Test]
        public void Constructor_ClampsValuesToRange()
        {
            // Arrange & Act
            var traits = new Traits(
                aggression: 1.5f,  // Should clamp to 1.0
                loyalty: -0.5f,     // Should clamp to 0.0
                anxiety: 0.5f,
                intelligence: 2.0f, // Should clamp to 1.0
                greed: -1.0f,       // Should clamp to 0.0
                bravery: 0.8f
            );
            
            // Assert
            Assert.AreEqual(1.0f, traits.Aggression, 0.001f, "Aggression should be clamped to 1.0");
            Assert.AreEqual(0.0f, traits.Loyalty, 0.001f, "Loyalty should be clamped to 0.0");
            Assert.AreEqual(0.5f, traits.Anxiety, 0.001f, "Anxiety should stay at 0.5");
            Assert.AreEqual(1.0f, traits.Intelligence, 0.001f, "Intelligence should be clamped to 1.0");
            Assert.AreEqual(0.0f, traits.Greed, 0.001f, "Greed should be clamped to 0.0");
            Assert.AreEqual(0.8f, traits.Bravery, 0.001f, "Bravery should stay at 0.8");
        }
        
        [Test]
        public void Constructor_WithDefaults_SetsReasonableValues()
        {
            // Arrange & Act
            var traits = new Traits();
            
            // Assert
            Assert.IsTrue(traits.Aggression >= 0f && traits.Aggression <= 1f);
            Assert.IsTrue(traits.Loyalty >= 0f && traits.Loyalty <= 1f);
            Assert.IsTrue(traits.Anxiety >= 0f && traits.Anxiety <= 1f);
            Assert.IsTrue(traits.Intelligence >= 0f && traits.Intelligence <= 1f);
            Assert.IsTrue(traits.Greed >= 0f && traits.Greed <= 1f);
            Assert.IsTrue(traits.Bravery >= 0f && traits.Bravery <= 1f);
        }
        
        [Test]
        public void Blend_HalfWay_ReturnsMiddleValues()
        {
            // Arrange
            var trait1 = new Traits(aggression: 0.0f, loyalty: 0.0f);
            var trait2 = new Traits(aggression: 1.0f, loyalty: 1.0f);
            
            // Act
            var blended = Traits.Blend(trait1, trait2, 0.5f);
            
            // Assert
            Assert.AreEqual(0.5f, blended.Aggression, 0.001f, "Should be halfway between 0 and 1");
            Assert.AreEqual(0.5f, blended.Loyalty, 0.001f, "Should be halfway between 0 and 1");
        }
        
        [Test]
        public void Blend_ZeroFactor_ReturnsFirstTrait()
        {
            // Arrange
            var trait1 = new Traits(aggression: 0.2f, loyalty: 0.3f);
            var trait2 = new Traits(aggression: 0.8f, loyalty: 0.9f);
            
            // Act
            var blended = Traits.Blend(trait1, trait2, 0.0f);
            
            // Assert
            Assert.AreEqual(0.2f, blended.Aggression, 0.001f, "Should return first trait values");
            Assert.AreEqual(0.3f, blended.Loyalty, 0.001f, "Should return first trait values");
        }
        
        [Test]
        public void Blend_OneFactor_ReturnsSecondTrait()
        {
            // Arrange
            var trait1 = new Traits(aggression: 0.2f, loyalty: 0.3f);
            var trait2 = new Traits(aggression: 0.8f, loyalty: 0.9f);
            
            // Act
            var blended = Traits.Blend(trait1, trait2, 1.0f);
            
            // Assert
            Assert.AreEqual(0.8f, blended.Aggression, 0.001f, "Should return second trait values");
            Assert.AreEqual(0.9f, blended.Loyalty, 0.001f, "Should return second trait values");
        }
        
        [Test]
        public void Blend_ClampsFactorToRange()
        {
            // Arrange
            var trait1 = new Traits(aggression: 0.2f);
            var trait2 = new Traits(aggression: 0.8f);
            
            // Act
            var blended1 = Traits.Blend(trait1, trait2, -0.5f); // Should clamp to 0
            var blended2 = Traits.Blend(trait1, trait2, 1.5f);  // Should clamp to 1
            
            // Assert
            Assert.AreEqual(0.2f, blended1.Aggression, 0.001f, "Negative factor should clamp to 0 (return first trait)");
            Assert.AreEqual(0.8f, blended2.Aggression, 0.001f, "Factor > 1 should clamp to 1 (return second trait)");
        }
        
        [Test]
        public void ToString_ReturnsAllTraitValues()
        {
            // Arrange
            var traits = new Traits(0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f);
            
            // Act
            var str = traits.ToString();
            
            // Assert
            Assert.IsTrue(str.Contains("0.10") || str.Contains("0.1"), "Should contain aggression value");
            Assert.IsTrue(str.Contains("0.20") || str.Contains("0.2"), "Should contain loyalty value");
            Assert.IsTrue(str.Contains("Traits"), "Should contain 'Traits'");
        }
        
        [Test]
        public void AllTraits_IndependentlyMutable()
        {
            // Arrange
            var traits = new Traits(
                aggression: 0.1f,
                loyalty: 0.2f,
                anxiety: 0.3f,
                intelligence: 0.4f,
                greed: 0.5f,
                bravery: 0.6f
            );
            
            // Assert - verify all 6 traits are set independently
            Assert.AreEqual(0.1f, traits.Aggression, 0.001f);
            Assert.AreEqual(0.2f, traits.Loyalty, 0.001f);
            Assert.AreEqual(0.3f, traits.Anxiety, 0.001f);
            Assert.AreEqual(0.4f, traits.Intelligence, 0.001f);
            Assert.AreEqual(0.5f, traits.Greed, 0.001f);
            Assert.AreEqual(0.6f, traits.Bravery, 0.001f);
        }
        
        [Test]
        public void Blend_WorksForAllTraits()
        {
            // Arrange
            var trait1 = new Traits(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
            var trait2 = new Traits(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f);
            
            // Act
            var blended = Traits.Blend(trait1, trait2, 0.5f);
            
            // Assert
            Assert.AreEqual(0.5f, blended.Aggression, 0.001f);
            Assert.AreEqual(0.5f, blended.Loyalty, 0.001f);
            Assert.AreEqual(0.5f, blended.Anxiety, 0.001f);
            Assert.AreEqual(0.5f, blended.Intelligence, 0.001f);
            Assert.AreEqual(0.5f, blended.Greed, 0.001f);
            Assert.AreEqual(0.5f, blended.Bravery, 0.001f);
        }
    }
}
