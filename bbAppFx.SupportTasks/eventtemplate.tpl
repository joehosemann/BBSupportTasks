<LPHEADER>
<HTML>
<HEAD>
  <!-- All references are remotely hosted so that the HTML is entirely portable. -->
<link rel="stylesheet" type="text/css" href="http://cdn.datatables.net/1.9.4/css/jquery.dataTables.css">
<link rel="stylesheet" type="text/css" href="http://www.datatables.net/release-datatables/extras/ColVis/media/css/ColVis.css">
<!--<link rel="stylesheet" type="text/css" href="https://dl.dropboxusercontent.com/u/1258269/BlackbaudProjects/PerformanceLog/css.css">-->
<link rel="stylesheet" type="text/css" href="https://dl.dropboxusercontent.com/u/1258269/BlackbaudProjects/PerformanceLog/jquery-ui-1.10.4.custom.min.css">
<script type="text/javascript" language="javascript" src="http://ajax.googleapis.com/ajax/libs/jquery/1.10.2/jquery.min.js"></script>
<script type="text/javascript" language="javascript" src="https://dl.dropboxusercontent.com/u/1258269/BlackbaudProjects/PerformanceLog/jquery-ui-1.10.4.custom.min.js"></script>
<script type="text/javascript" language="javascript" src="http://cdn.datatables.net/1.9.4/js/jquery.dataTables.js"></script>
<script type="text/javascript" language="javascript" src="http://www.datatables.net/extras/thirdparty/ColumnFilterWidgets/DataTables/extras/ColumnFilterWidgets/media/js/ColumnFilterWidgets.js"></script>
<script type="text/javascript" language="javascript" src="https://dl.dropboxusercontent.com/u/1258269/BlackbaudProjects/PerformanceLog/colvis.js"></script>

<script>
$(document).ready(function () {
    var oTable = $('#MYGRID').dataTable({
		"aoColumnDefs": [
			{ "bVisible": false, "aTargets": [ 0, 11 ] }
		],
		"iDisplayLength": 50,
		"sDom": 'WR<"clear">Clfrtip',
		"oColumnFilterWidgets": {
			"aiExclude": [0,1,2,3,8,10,11,12,13,14,15]
        }
    });
});
</script>
<style>
body, select, input, option {
    font-family: "Trebuchet MS", Helvetica, sans-serif;
    font-size: 12px;
}
thead tr {
	border-top: 0px;
	border-left: 0px;
	border-right: 0px;
	border-bottom: 1px;
	border-color: gray;
	border-spacing: 2px;
}
tr {
	font-family: "Trebuchet MS", Helvetica, sans-serif;
    font-size: 12px;
}
td {
	border: 0px;
}
table.dataTable thead th {
	border-top: 0px;
	border-left: 0px;
	border-right: 0px;
	border-bottom: 1px;
	border-color: gray;
	border-spacing: 2px;
}
.column-filter-widget {
display: inline;
}
body {
	font: 80%/1.45em "Lucida Grande", Verdana, Arial, Helvetica, sans-serif;
	margin: 0;
	padding: 0;
	color: #333;
	background-color: #fff;
	border: 0px;
}
.FixedHeader_Cloned th {
	background-color: white;
}
th, td {
	height: 30px;
}
table {
	border: 0px;
	border-spacing: 0px;
}
thead {
	border-top: 0px;
	border-left: 0px;
	border-right: 0px;
	border-bottom: 1px solid gray;
	border-color: gray;
	border-spacing: 2px;
}
</style>

</HEAD>
<BODY id="bodyid">

<TABLE ID="MYGRID" BORDERCOLOR="BLACK" BORDER="1" CELLPADDING="2" CELLSPACING="2">
<THEAD><TR>
<TH ALIGN=LEFT BGCOLOR="#C0C0C0">EventLog</TH> <TH ALIGN=LEFT BGCOLOR="#C0C0C0">RecordNumber</TH> <TH ALIGN=LEFT BGCOLOR="#C0C0C0">TimeGenerated</TH> <TH ALIGN=LEFT BGCOLOR="#C0C0C0">TimeWritten</TH> <TH ALIGN=LEFT BGCOLOR="#C0C0C0">EventID</TH> <TH ALIGN=LEFT BGCOLOR="#C0C0C0">EventType</TH> <TH ALIGN=LEFT BGCOLOR="#C0C0C0">EventTypeName</TH> <TH ALIGN=LEFT BGCOLOR="#C0C0C0">EventCategory</TH> <TH ALIGN=LEFT BGCOLOR="#C0C0C0">EventCategoryName</TH> <TH ALIGN=LEFT BGCOLOR="#C0C0C0">SourceName</TH> <TH ALIGN=LEFT BGCOLOR="#C0C0C0">Strings</TH> <TH ALIGN=LEFT BGCOLOR="#C0C0C0">ComputerName</TH> <TH ALIGN=LEFT BGCOLOR="#C0C0C0">SID</TH> <TH ALIGN=LEFT BGCOLOR="#C0C0C0">Message</TH> <TH ALIGN=LEFT BGCOLOR="#C0C0C0">Data</TH>
</TR></THEAD>
</LPHEADER>
<TBODY>
<LPBODY>
<TR>
  <TD>%EventLog%</TD> <TD>%RecordNumber%</TD> <TD>%TimeGenerated%</TD> <TD>%TimeWritten%</TD> <TD>%EventID%</TD> <TD>%EventType%</TD> <TD>%EventTypeName%</TD> <TD>%EventCategory%</TD> <TD>%EventCategoryName%</TD> <TD>%SourceName%</TD> <TD>%Strings%</TD> <TD>%ComputerName%</TD> <TD>%SID%</TD> <TD>%Message%</TD> <TD>%Data%</TD>
</TR>
</LPBODY>
</TBODY>
</TABLE>
<script>
	$('.FixedHeader_Cloned').css('top','78px');
	</script>
</BODY>
</HTML>