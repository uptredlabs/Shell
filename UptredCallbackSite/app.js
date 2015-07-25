function gup(name) {
    var url = location.href;
    name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
    var regexS = "[\\?&]" + name + "=([^&#]*)";
    var regex = new RegExp(regexS);
    var results = regex.exec(url);
    return results == null ? null : results[1];
}

window.onload = function () {
    var code = gup("code");
    var el = document.getElementById('codebox');
    el.value = code;
};
//# sourceMappingURL=app.js.map
