using NUnit.Framework;
using UnityEngine;

namespace Volleyball.Tests
{
    public class WebPlatformTests
    {
        [Test] public void Controls_WebTouchOnly_DesktopWebHidden()
        {
            Assert.True(WebPlatformPolicy.ShowControls(true, true, false, false, false));
            Assert.False(WebPlatformPolicy.ShowControls(true, false, true, false, false));
            Assert.True(WebPlatformPolicy.ShowControls(false, false, true, false, false));
            Assert.True(WebPlatformPolicy.ShowControls(false, false, false, true, true));
        }
        [Test] public void Landscape_PortraitTouchOnly_ShowsPrompt()
        {
            Assert.True(WebPlatformPolicy.NeedsLandscape(true, 390, 844));
            Assert.False(WebPlatformPolicy.NeedsLandscape(true, 844, 390));
            Assert.False(WebPlatformPolicy.NeedsLandscape(false, 390, 844));
        }
        [Test] public void SafeArea_WebTouchRetainsInsets_OfflineAreaUnchanged()
        {
            Rect fallback = WebPlatformPolicy.SafeArea(new Rect(0,0,1,1),true);
            Assert.That(fallback.xMin, Is.EqualTo(.02f).Within(.00001f));
            Assert.That(fallback.yMin, Is.EqualTo(.02f).Within(.00001f));
            Assert.That(fallback.xMax, Is.EqualTo(.98f).Within(.00001f));
            Assert.That(fallback.yMax, Is.EqualTo(.98f).Within(.00001f));
            var inset = new Rect(.1f,.1f,.8f,.8f);
            Assert.AreEqual(inset, WebPlatformPolicy.SafeArea(inset,true));
            Assert.AreEqual(new Rect(0,0,1,1), WebPlatformPolicy.SafeArea(new Rect(0,0,1,1),false));
        }
    }
}
