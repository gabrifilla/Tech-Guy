using NUnit.Framework;
using UnityEngine;

public sealed class NexusCardLayoutTests
{
    [TestCase(1280, 720, 236)]
    [TestCase(1280, 720, 320)]
    [TestCase(1920, 1080, 236)]
    [TestCase(1920, 1080, 320)]
    public void CardClearsAvatarAndPersistentControls(int width, int height, int cardHeight)
    {
        float scale = Mathf.Min(width / 1280f, height / 720f);
        var reference = new Vector2(width / scale, height / scale);
        foreach (var position in new[] { new Vector2(640, 360), new Vector2(240, 500), new Vector2(1100, 500) })
        {
            Rect rect = WeaponCardView.PlaceCard(reference, cardHeight, position);
            Assert.IsFalse(rect.Overlaps(new Rect(position.x - 68, position.y - 135, 136, 190)));
            Assert.IsFalse(rect.Overlaps(new Rect(24, 24, 370, 116)));
            Assert.IsFalse(rect.Overlaps(new Rect(999, 24, 241, 45)));
            Assert.GreaterOrEqual(rect.xMin, 0); Assert.GreaterOrEqual(rect.yMin, 0);
            Assert.LessOrEqual(rect.xMax, reference.x); Assert.LessOrEqual(rect.yMax, reference.y);
        }
    }
}
