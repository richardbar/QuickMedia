/**
* QuickMedia, an open source media server
* Copyright (C) 2020  Richard Bariampa
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU Affero General Public License as published
* by the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU Affero General Public License for more details.
* You should have received a copy of the GNU Affero General Public License
* along with this program.  If not, see <https://www.gnu.org/licenses/>.
*
* richardbar:          richard1996ba@gmail.com
**/

function isString(value) {
	return typeof value == 'string' || value instanceof String;
}

function isNumber (value) {
	return typeof value === 'number' && isFinite(value);
}

function isNull (value) {
	return value === null;
}

function isUndefined (value) {
	return typeof value === 'undefined';
}

const AlertLevels = Object.freeze({
	'info': 0,
	'error': 1
});

function showAlert(message, duration, level) {
	try {
		if (!(
		isString(message) && 
		(isNumber(duration) || isNull(duration)) && 
		isNumber(level)))
		return false;
	
	$('#alert_m').text(message);
	
	var lastClass = $('#alert_m')[0].className.split(' ')[1];
	var nextClass;
	
	switch (level) {
		case 0:
			$('.cAlert')[0].style.cursor = 'default';
			$('.cAlert')[0].onclick = null;
			nextClass = 'info';
			break;
		case 1:
			$('body > *:not(.cAlert)').css('filter', 'blur(5px)');
			$('.cAlert')[0].style.cursor = 'pointer';
			$('.cAlert')[0].onclick = (() => {
				$('.cAlert').css('display', 'none');
				$('body > *:not(.cAlert)').css('filter', 'blur(0px)');
			});
			nextClass = 'danger';
			break;
		default:
			return false;
	}
	
	if (isUndefined(lastClass))
		$('#alert_m').addClass('alert-' + nextClass);
	else
		$('#alert_m').toggleClass(lastClass + ' alert-' + nextClass);
	
	$('.cAlert').css('display', 'block');
	if (!isNull(duration))
	new Promise(r => setTimeout(r, duration)).then(() => $('.cAlert').css('display', 'none'));
	return true;
	}
	catch {
		return false;
	}
}

async function getFromUrl(_url) {
	var response = await $.ajax({
		url: _url,
		type: "GET",
		crossDomain: true,
		async: false,
		headers: {
			'Access-Control-Allow-Origin': '*'
		}
	});
	return response;
}