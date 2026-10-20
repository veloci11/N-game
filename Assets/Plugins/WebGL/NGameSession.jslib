mergeInto(LibraryManager.library, {
  NGameSaveAccess: function(json) {
    try {
      var value = UTF8ToString(json);
      if (value) sessionStorage.setItem('ngame.room-access', value);
      else sessionStorage.removeItem('ngame.room-access');
    } catch (_) {}
  },
  NGameFreeAccess: function(pointer) { _free(pointer); },
  NGameReadAccess: function() {
    var value = '';
    try { value = sessionStorage.getItem('ngame.room-access') || ''; } catch (_) {}
    var size = lengthBytesUTF8(value) + 1;
    var result = _malloc(size);
    stringToUTF8(value, result, size);
    return result;
  }
});
