mergeInto(LibraryManager.library, {
  VolleyballHasTouch: function () { return (navigator.maxTouchPoints || 0) > 0 ? 1 : 0; }
});
