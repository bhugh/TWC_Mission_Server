// JavaScript Document
$(document).ready(function() {
  $(".searchInput").keyup(function () {
      //split the current value of searchInput
      var data = this.value.toUpperCase().split(" ");
      //create a jquery object of the rows
      var jo = $("#filterable").find("tr");
      if (this.value == "") {
          jo.show();
          return;
      }
      //hide all the rows
      jo.hide();
  
      //Recusively filter the jquery object to get results.
      jo.filter(function (i, v) {
          var $t = $(this);
          for (var d = 0; d < data.length; ++d) {
              //if ($t.is(":contains('" + data[d] + "')")) {      //case sensitive
              if ($t.text().toUpperCase().indexOf(data[d]) > -1) { //case insensitive but input must be uppercased-first              
                  return true;
              }
          }
          return false;
      })
      //show the rows that match.
      .show();
  }).focus(function () {
      this.value = "";
      $(this).css({
          "color": "black"
      });
      $(this).unbind('focus');
  }).css({
      "color": "#C0C0C0"
  });
});