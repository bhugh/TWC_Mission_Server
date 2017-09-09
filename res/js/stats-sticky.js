// JavaScript Document
$(document).ready(function() {
		$(document).ready(function () {
			// initialize stickyTableHeaders _after_ tablesorter
			$(".tablesorter").tablesorter({ sortList:[[5,1]] });
			$("table").stickyTableHeaders();
		});
	
});