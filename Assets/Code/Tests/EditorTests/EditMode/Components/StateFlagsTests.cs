using NUnit.Framework;

namespace ProjectVice.Tests.Components
{
    /// <summary>
    /// Unit tests for StateFlags component
    /// Тесты для компонента StateFlags
    /// </summary>
    public class StateFlagsTests
    {
        [Test]
        public void Constructor_SetsInitialFlags()
        {
            // Arrange & Act
            var states = new StateFlags(
                alive: true,
                injured: false,
                arrested: true,
                dead: false,
                wanted: true,
                inVehicle: false,
                sleeping: true,
                busy: false
            );
            
            // Assert
            Assert.IsTrue(states.IsAlive);
            Assert.IsFalse(states.IsInjured);
            Assert.IsTrue(states.IsArrested);
            Assert.IsFalse(states.IsDead);
            Assert.IsTrue(states.IsWanted);
            Assert.IsFalse(states.IsInVehicle);
            Assert.IsTrue(states.IsSleeping);
            Assert.IsFalse(states.IsBusy);
        }
        
        [Test]
        public void SetFlag_ChangesState()
        {
            // Arrange
            var states = new StateFlags(alive: true);
            
            // Act
            states.IsWanted = true;
            
            // Assert
            Assert.IsTrue(states.IsWanted, "Setting IsWanted to true should work");
        }
        
        [Test]
        public void ClearFlag_RemovesState()
        {
            // Arrange
            var states = new StateFlags(alive: true, wanted: true);
            
            // Act
            states.IsWanted = false;
            
            // Assert
            Assert.IsFalse(states.IsWanted, "Setting IsWanted to false should work");
            Assert.IsTrue(states.IsAlive, "Other flags should remain unchanged");
        }
        
        [Test]
        public void HasAnyFlag_WithMatchingFlag_ReturnsTrue()
        {
            // Arrange
            var states = new StateFlags(alive: true, wanted: true);
            var checkFlags = new StateFlags(wanted: true);
            
            // Act
            var hasAny = states.HasAnyFlag(checkFlags);
            
            // Assert
            Assert.IsTrue(hasAny, "Should return true when at least one flag matches");
        }
        
        [Test]
        public void HasAnyFlag_WithNoMatchingFlags_ReturnsFalse()
        {
            // Arrange
            var states = new StateFlags(alive: true);
            var checkFlags = new StateFlags(dead: true, arrested: true);
            
            // Act
            var hasAny = states.HasAnyFlag(checkFlags);
            
            // Assert
            Assert.IsFalse(hasAny, "Should return false when no flags match");
        }
        
        [Test]
        public void HasAllFlags_WithAllMatching_ReturnsTrue()
        {
            // Arrange
            var states = new StateFlags(alive: true, wanted: true, busy: true);
            var checkFlags = new StateFlags(wanted: true, busy: true);
            
            // Act
            var hasAll = states.HasAllFlags(checkFlags);
            
            // Assert
            Assert.IsTrue(hasAll, "Should return true when all checked flags are set");
        }
        
        [Test]
        public void HasAllFlags_WithSomeMissing_ReturnsFalse()
        {
            // Arrange
            var states = new StateFlags(alive: true, wanted: true);
            var checkFlags = new StateFlags(wanted: true, arrested: true);
            
            // Act
            var hasAll = states.HasAllFlags(checkFlags);
            
            // Assert
            Assert.IsFalse(hasAll, "Should return false when not all checked flags are set");
        }
        
        [Test]
        public void MultipleFlags_CanBeSetSimultaneously()
        {
            // Arrange & Act
            var states = new StateFlags();
            states.IsAlive = true;
            states.IsWanted = true;
            states.IsInVehicle = true;
            
            // Assert
            Assert.IsTrue(states.IsAlive);
            Assert.IsTrue(states.IsWanted);
            Assert.IsTrue(states.IsInVehicle);
        }
        
        [Test]
        public void ToString_ReturnsActiveFlagsOnly()
        {
            // Arrange
            var states = new StateFlags(alive: true, wanted: true);
            
            // Act
            var str = states.ToString();
            
            // Assert
            Assert.IsTrue(str.Contains("Alive"), "Should contain Alive");
            Assert.IsTrue(str.Contains("Wanted"), "Should contain Wanted");
            Assert.IsFalse(str.Contains("Dead"), "Should not contain Dead");
            Assert.IsFalse(str.Contains("Arrested"), "Should not contain Arrested");
        }
        
        [Test]
        public void ToString_NoFlags_ReturnsNoFlags()
        {
            // Arrange
            var states = new StateFlags();
            
            // Act
            var str = states.ToString();
            
            // Assert
            Assert.AreEqual("NoFlags", str, "Empty state should return 'NoFlags'");
        }
        
        [Test]
        public void AllFlags_CanBeSetAndCleared()
        {
            // Arrange
            var states = new StateFlags();
            
            // Act - Set all
            states.IsAlive = true;
            states.IsInjured = true;
            states.IsArrested = true;
            states.IsDead = true;
            states.IsWanted = true;
            states.IsInVehicle = true;
            states.IsSleeping = true;
            states.IsBusy = true;
            
            // Assert all set
            Assert.IsTrue(states.IsAlive);
            Assert.IsTrue(states.IsInjured);
            Assert.IsTrue(states.IsArrested);
            Assert.IsTrue(states.IsDead);
            Assert.IsTrue(states.IsWanted);
            Assert.IsTrue(states.IsInVehicle);
            Assert.IsTrue(states.IsSleeping);
            Assert.IsTrue(states.IsBusy);
            
            // Act - Clear all
            states.IsAlive = false;
            states.IsInjured = false;
            states.IsArrested = false;
            states.IsDead = false;
            states.IsWanted = false;
            states.IsInVehicle = false;
            states.IsSleeping = false;
            states.IsBusy = false;
            
            // Assert all cleared
            Assert.IsFalse(states.IsAlive);
            Assert.IsFalse(states.IsInjured);
            Assert.IsFalse(states.IsArrested);
            Assert.IsFalse(states.IsDead);
            Assert.IsFalse(states.IsWanted);
            Assert.IsFalse(states.IsInVehicle);
            Assert.IsFalse(states.IsSleeping);
            Assert.IsFalse(states.IsBusy);
        }
        
        [Test]
        public void Flags_AreIndependent()
        {
            // Arrange & Act
            var states = new StateFlags();
            states.IsAlive = true;
            states.IsWanted = true;
            
            // Change one flag
            states.IsWanted = false;
            
            // Assert
            Assert.IsTrue(states.IsAlive, "Other flags should not be affected");
            Assert.IsFalse(states.IsWanted);
        }
    }
}
