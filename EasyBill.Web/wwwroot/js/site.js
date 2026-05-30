//$(document)(function () {
//    $("select").each(function () {
//        $(this).wrap("<div class='dropdown-wrapper' style='position: relative; display: inline-block; width: 100%;'></div>");
//        $(this).after("<i class='fa fa-chevron-down dropdown-icon' style='position: absolute; right: 10px; top: 50%; transform: translateY(-50%); pointer-events: none;'></i>");
//        $(this).css({
//            "appearance": "none",
//            "-webkit-appearance": "none",
//            "-moz-appearance": "none",
//            "padding-right": "35px"
//        });
//    });
//});

 
  
  $.ajaxSetup({
      beforeSend: function (xhr) {
          xhr.setRequestHeader("Authorization", "Bearer " + localStorage.getItem("jwtToken"));
      },
      complete: function (xhr) {
          var newToken = xhr.getResponseHeader("X-New-JWT-Token");
          if (newToken) {
              localStorage.setItem("jwtToken", newToken);
          }
      }
  }); 

 