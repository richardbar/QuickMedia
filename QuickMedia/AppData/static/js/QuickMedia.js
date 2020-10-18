var pages = ['index', 'photos', 'videos', 'audios'];

const cols = 4;

function ShowIndex() {
    return true;
}

function ShowPhotos() {
    $.post('getMedias', { type: 'photo' }, function (data) {
        data = JSON.parse(data);
        const width = document.getElementById('content').getBoundingClientRect().width;
        if (data.length == 0)
            return;
        console.log(data.length);
        var rows = Math.ceil(data.length / cols);
        console.log(rows);

        var contentInnerHTML = '<div class="container">';

        for (var i = 0; i < rows; i++) {
            contentInnerHTML += '<div class="row">';

            for (var j = i * cols; j < Math.min(data.length, (i + 1) * cols); j++) {
                contentInnerHTML += '<div class="col-3" style="background-color: #EEEEEE; margin: 0 auto;"><img src="' + data[j]['fileName'] + '" width=100%></div>';
            }

            contentInnerHTML += '</div>';
        }

        contentInnerHTML += '</div>';

        document.getElementById('content').innerHTML = contentInnerHTML;
    });

    return true;
}

function ShowVideos() {
    return true;
}

function ShowAudios() {
    return true;
}

/**
 * @param {String} page
 * @returns {Boolean}
 */
function ShowPage(page) {
    const functionName = 'Show' + page.charAt(0).toUpperCase() + page.slice(1);
    if (typeof window[functionName] === 'function') {
        if (!window[functionName]()) {
            console.error('There was an error trying to load ' + page);
            if (page != 'index')
                ShowPage('index');
            return false;
        }
        return true;
    }
    else {
        console.error('The function "' + functionName + '" does not exist');
        return false;
    }
}

function OnHashChange() {
    if (!window.location.hash)
        ShowPage('index');
    else {
        if (!ShowPage(window.location.hash.substring(1))) {
            window.location.hash = '#index';
            ShowPage('index');
        }
    }
}

function OnWindowResize() {

}

(function() {
    window.onresize = OnWindowResize;
    window.onhashchange = OnHashChange;

    OnHashChange();
})();