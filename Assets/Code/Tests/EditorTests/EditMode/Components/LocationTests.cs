using NUnit.Framework;
using Unity.Mathematics;

namespace ProjectVice.Tests.Components
{
    /// <summary>
    /// Unit tests for Location component
    /// Тесты для компонента Location
    /// </summary>
    public class LocationTests
    {
        [Test]
        public void FromGlobal_2D_CalculatesCorrectChunkId()
        {
            // Arrange
            var globalPos = new float2(250f, 350f); // Should be in chunk (2, 3)
            
            // Act
            var location = Location.FromGlobal(globalPos);
            
            // Assert
            Assert.AreEqual(new int2(2, 3), location.ChunkId, "ChunkId should be (2, 3) for position (250, 350)");
        }
        
        [Test]
        public void FromGlobal_2D_CalculatesCorrectLocalPosition()
        {
            // Arrange
            var globalPos = new float2(250f, 350f);
            // Chunk (2, 3) starts at (200, 300), so local should be (50, 0, 50)
            
            // Act
            var location = Location.FromGlobal(globalPos);
            
            // Assert
            Assert.AreEqual(50f, location.PositionInChunk.x, 0.01f, "Local X should be 50");
            Assert.AreEqual(0f, location.PositionInChunk.y, 0.01f, "Local Y should be 0");
            Assert.AreEqual(50f, location.PositionInChunk.z, 0.01f, "Local Z should be 50");
        }
        
        [Test]
        public void FromGlobal_3D_PreservesHeight()
        {
            // Arrange
            var globalPos = new float3(150f, 25f, 250f);
            
            // Act
            var location = Location.FromGlobal(globalPos);
            
            // Assert
            Assert.AreEqual(25f, location.PositionInChunk.y, 0.01f, "Height should be preserved");
        }
        
        [Test]
        public void GlobalPosition2D_ReturnsCorrectPosition()
        {
            // Arrange
            var location = new Location(new int2(2, 3), new float3(50f, 10f, 60f));
            
            // Act
            var globalPos = location.GlobalPosition2D;
            
            // Assert
            Assert.AreEqual(250f, globalPos.x, 0.01f, "Global X should be 250");
            Assert.AreEqual(360f, globalPos.y, 0.01f, "Global Y should be 360");
        }
        
        [Test]
        public void GlobalPosition3D_ReturnsCorrectPosition()
        {
            // Arrange
            var location = new Location(new int2(2, 3), new float3(50f, 10f, 60f));
            
            // Act
            var globalPos = location.GlobalPosition3D;
            
            // Assert
            Assert.AreEqual(250f, globalPos.x, 0.01f, "Global X should be 250");
            Assert.AreEqual(10f, globalPos.y, 0.01f, "Global Y (height) should be 10");
            Assert.AreEqual(360f, globalPos.z, 0.01f, "Global Z should be 360");
        }
        
        [Test]
        public void UpdatePosition_2D_UpdatesChunkId()
        {
            // Arrange
            var location = new Location(new int2(0, 0), new float3(10f, 0f, 10f));
            
            // Act
            location.UpdatePosition(new float2(250f, 350f)); // Should move to chunk (2, 3)
            
            // Assert
            Assert.AreEqual(new int2(2, 3), location.ChunkId, "ChunkId should update to (2, 3)");
        }
        
        [Test]
        public void UpdatePosition_3D_PreservesHeight()
        {
            // Arrange
            var location = new Location(new int2(0, 0), new float3(10f, 5f, 10f));
            
            // Act
            location.UpdatePosition(new float3(250f, 15f, 350f));
            
            // Assert
            Assert.AreEqual(15f, location.PositionInChunk.y, 0.01f, "Height should be updated to 15");
        }
        
        [Test]
        public void ContainsPoint_InsideBounds_ReturnsTrue()
        {
            // Arrange
            var location = new Location(new int2(2, 3), new float3(0f, 0f, 0f));
            var point = new float2(250f, 350f); // Inside chunk (2, 3)
            
            // Act
            var contains = location.ContainsPoint(point);
            
            // Assert
            Assert.IsTrue(contains, "Point should be inside chunk bounds");
        }
        
        [Test]
        public void ContainsPoint_OutsideBounds_ReturnsFalse()
        {
            // Arrange
            var location = new Location(new int2(2, 3), new float3(0f, 0f, 0f));
            var point = new float2(150f, 150f); // Outside chunk (2, 3) - in chunk (1, 1)
            
            // Act
            var contains = location.ContainsPoint(point);
            
            // Assert
            Assert.IsFalse(contains, "Point should be outside chunk bounds");
        }
        
        [Test]
        public void Constructor_NegativeCoordinates_WorksCorrectly()
        {
            // Arrange & Act
            var location = new Location(new int2(-1, -2), new float3(50f, 0f, 50f));
            var globalPos = location.GlobalPosition2D;
            
            // Assert
            Assert.AreEqual(new int2(-1, -2), location.ChunkId);
            Assert.AreEqual(-50f, globalPos.x, 0.01f, "Negative chunk should work correctly");
            Assert.AreEqual(-150f, globalPos.y, 0.01f, "Negative chunk should work correctly");
        }
        
        [Test]
        public void FromGlobal_NegativePosition_CalculatesCorrectChunk()
        {
            // Arrange
            var globalPos = new float2(-50f, -150f);
            
            // Act
            var location = Location.FromGlobal(globalPos);
            
            // Assert
            Assert.AreEqual(new int2(-1, -2), location.ChunkId, "Negative positions should calculate correct chunk");
        }
        
        [Test]
        public void ToString_ReturnsReadableFormat()
        {
            // Arrange
            var location = new Location(new int2(2, 3), new float3(50f, 10f, 60f));
            
            // Act
            var str = location.ToString();
            
            // Assert
            Assert.IsTrue(str.Contains("2") && str.Contains("3"), "ToString should contain chunk coordinates");
            Assert.IsTrue(str.Contains("50") || str.Contains("60"), "ToString should contain local position");
        }
    }
}
