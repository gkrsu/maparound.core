function GetElemTopLeft(elem) 
{
    var top = 0, left = 0
    if (elem.getBoundingClientRect) {

        var box = elem.getBoundingClientRect()

        var body = document.body
        var docElem = document.documentElement

        var scrollTop = window.pageYOffset || docElem.scrollTop || body.scrollTop
        var scrollLeft = window.pageXOffset || docElem.scrollLeft || body.scrollLeft

        var clientTop = docElem.clientTop || body.clientTop || 0
        var clientLeft = docElem.clientLeft || body.clientLeft || 0

        top = box.top + scrollTop - clientTop
        left = box.left + scrollLeft - clientLeft

        return { top: Math.round(top), left: Math.round(left) };
    }
    else {
        while (elem) {
            top = top + parseInt(elem.offsetTop)
            left = left + parseInt(elem.offsetLeft)
            elem = elem.offsetParent
        }

        return { top: top, left: left }
    }
}

function DefPosition(event) {
    var x = y = 0;
    if (document.attachEvent != null) {
        x = event.clientX + document.documentElement.scrollLeft + document.body.scrollLeft;
        y = event.clientY + document.documentElement.scrollTop + document.body.scrollTop;
    }
    if (!document.attachEvent && document.addEventListener) {
        x = event.clientX;
        y = event.clientY;
    }
    return { x: x, y: y };
}

function PositionOnElement(event, elem) 
{
    var x = DefPosition(event).x - GetElemTopLeft(elem).left;
    var y = DefPosition(event).y - GetElemTopLeft(elem).top;

    var scrollTop = window.pageYOffset || document.documentElement.scrollTop || document.body.scrollTop;
    var scrollLeft = window.pageXOffset || document.documentElement.scrollLeft || document.body.scrollLeft;

    if (typeof window.pageXOffset != 'undefined' || document.body.scrollTop > 0 ||
                    document.body.scrollLeft > 0) {
        if (navigator.appName != 'Opera') {
            x += scrollLeft;
            y += scrollTop;
        }
    }
    return { x: x, y: y }
}

var mapClientControl = function(mapInit) {

    this.isIE = navigator.appName == "Microsoft Internet Explorer";

    this.elById =
    function(id) {
        return document.getElementById(id);
    }

    this.mapId = mapInit.id;
    this.variableName = mapInit.variableName;
    this.contourDrawer = mapInit.contourDrawer;
    this.lineDrawer = mapInit.lineDrawer;

    this.lineDrawingCanvas = this.elById(mapInit.lineDrawingCanvasId);
    this.contourDrawingCanvas = this.elById(mapInit.contourDrawingCanvasId);
    this.measureResult = this.elById(mapInit.measureResultId);

    this.layerControlEnabled = mapInit.layerControlEnabled;

    this.setOpacity =
                function(el, opacity) {
                    if (el.style.opacity != null)
                        el.style.opacity = opacity.toString();
                    if (el.style.filter != null)
                        el.style.filter = 'alpha(opacity = ' + (opacity * 100).toString() + ')';
                }

    this.DraggingObjectLeft = 0;
    this.DraggingObjectTop = 0;
    this.cancelDrag = false;
    this.oldCursor = null;

    this.requestProcessing = false;

    this.SelectedItem = null;
    this.selectedToolName = mapInit.toolName;
    this.MouseX = 0;
    this.MouseY = 0;
    this.mapDX = 0;
    this.mapDY = 0;
    this.xValue = 0;
    this.yValue = 0;

    this.shapeCoords = '';
    this.infoStr = '';
    this.imgLoader = this.elById(mapInit.imgLoaderId);
    this.layersControl = this.elById(mapInit.layersControlId);
    this.layersSwitchButton = this.elById(mapInit.layersSwitchButtonId);
    this.layerRecordsHolder = this.elById(mapInit.layerRecordsHolderId);

    this.scaleLabel = this.elById(mapInit.scaleLabelId);
    this.scaleSegment = this.elById(mapInit.scaleSegmentId);
    this.toolBar = this.elById(mapInit.toolBarId)

    this.dragButton = this.elById(mapInit.dragButtonId);
    this.selectButton = this.elById(mapInit.selectButtonId);
    this.drawLineButton = this.elById(mapInit.drawLineButtonId);
    this.drawContourButton = this.elById(mapInit.drawContourButtonId);

    this.dragButtonEnabledUrl = mapInit.dragButtonEnabledUrl;
    this.dragButtonDisabledUrl = mapInit.dragButtonDisabledUrl;
    this.selectButtonEnabledUrl = mapInit.selectButtonEnabledUrl;
    this.selectButtonDisabledUrl = mapInit.selectButtonDisabledUrl;
    this.drawLineButtonEnabledUrl = mapInit.drawLineButtonEnabledUrl;
    this.drawLineButtonDisabledUrl = mapInit.drawLineButtonDisabledUrl;
    this.drawContourButtonEnabledUrl = mapInit.drawContourButtonEnabledUrl;
    this.drawContourButtonDisabledUrl = mapInit.drawContourButtonDisabledUrl;

    this.dragCursorUrl = mapInit.dragCursorUrl;
    this.drawLineCursorUrl = mapInit.drawLineCursorUrl;
    this.drawContourCursorUrl = mapInit.drawContourCursorUrl;

    this.objectInfo = this.elById(mapInit.objectInfoId);
    this.info = this.elById(mapInit.infoId);

    this.disableAllTools =
        function() {
            this.dragButton.src = this.dragButtonDisabledUrl;
            this.selectButton.src = this.selectButtonDisabledUrl;
            this.drawLineButton.src = this.drawLineButtonDisabledUrl;
            this.drawContourButton.src = this.drawContourButtonDisabledUrl;
        };

    this.selectTool =
        function(toolName, cursorName, buttonId) {
            this.disableAllTools();
            this.img1.style.cursor = cursorName;
            this.img2.style.cursor = cursorName;
            if (toolName == 'lineMeasurer') {
                this.lineDrawingCanvas.style.left = '0px';
                this.lineDrawingCanvas.style.top = '0px';
                this.lineDrawingCanvas.style.display = 'block';
            }
            else
                this.lineDrawingCanvas.style.display = 'none';

            if (toolName == 'areaMeasurer') {
                this.contourDrawingCanvas.style.left = '0px';
                this.contourDrawingCanvas.style.top = '0px';
                this.contourDrawingCanvas.style.display = 'block';
            }
            else
                this.contourDrawingCanvas.style.display = 'none';

            this.selectedToolName = toolName;
            this.measureResult.style.visibility = 'hidden';
        }

    this.selectSelectTool =
        function() {
            this.selectTool('info', 'pointer', '');
            this.selectButton.src = this.selectButtonEnabledUrl;
        }

    this.selectDragTool =
        function selectDragTool() {
            var cur = 'url(' + this.dragCursorUrl + '), default';
            if (navigator.appName == 'Opera') cur = 'default';
            this.selectTool('drag', cur, '');
            this.dragButton.src = this.dragButtonEnabledUrl;
        }

    this.selectLineMeasurerTool =
        function() {
            this.lineDrawer.Polyline = new Array();
            this.lineDrawer.redrawAll();

            var cur = 'url(' + this.drawLineCursorUrl + '), default';
            if (navigator.appName == 'Opera') cur = 'default';
            this.selectTool('lineMeasurer', cur, '');
            this.drawLineButton.src = this.drawLineButtonEnabledUrl;
        }

    this.selectAreaMeasurerTool =
        function() {
            this.contourDrawer.Polygon = new Array();
            this.contourDrawer.redrawAll();

            var cur = 'url(' + this.drawContourCursorUrl + '), default';
            if (navigator.appName == 'Opera') cur = 'default';
            this.selectTool('areaMeasurer', cur, '');
            this.drawContourButton.src = this.drawContourButtonEnabledUrl;
        }

    this.switchTo =
        function(toolname) {
            if (toolname == 'drag') this.selectDragTool();
            if (toolname == 'info') this.selectSelectTool();
            if (toolname == 'lineMeasurer') this.selectLineMeasurerTool();
            if (toolname == 'areaMeasurer') this.selectAreaMeasurerTool();
        }

    this.toolBarOpacity = 0.65;

    this.stepFadeToolbar =
            function() {
                var el = this.toolBar;
                var op = this.toolBarOpacity;
                if (this.toolBarFadeStep < 0) {
                    if (op > 0.65) {
                        this.setOpacity(el, op);
                        op += this.toolBarFadeStep;
                        this.toolBarOpacity = op;
                        setTimeout(this.variableName + '.stepFadeToolbar()', 40);
                    }
                    else {
                        this.toolBarFadeStep = 0;
                        this.setOpacity(el, 0.65);
                        this.toolBarOpacity = 0.65;
                    }
                }
                else {
                    if (op < 0.93) {
                        this.setOpacity(el, op);
                        op += this.toolBarFadeStep;
                        this.toolBarOpacity = op;
                        setTimeout(this.variableName + '.stepFadeToolbar()', 40);
                    }
                    else {
                        this.toolBarFadeStep = 0;
                        this.setOpacity(el, 0.95);
                        this.toolBarOpacity = 0.95;
                    }
                }
            }

    this.fadeOutToolbar =
        function(e) {
            var ev = window.event || e;
            var tg = (ev.relatedTarget) ? ev.relatedTarget : ev.toElement;
            if (tg.nodeName == 'IMG' || tg.nodeName == 'shape' || tg.nodeName == 'CANVAS') {
                this.toolBarFadeStep = -0.02;
                this.stepFadeToolbar();
            }
        }

    this.fadeInToolbar =
        function(e) {
            var ev = window.event || e;
            var tg = (ev.relatedTarget) ? ev.relatedTarget : ev.fromElement;
            this.toolBarFadeStep = 0.03;
            this.stepFadeToolbar();
        }

    this.mapImageLoading =
            function(isMapLoading) {
                var visibility = 'hidden';
                if (isMapLoading) {
                    visibility = 'visible';
                }
                else {

                    if (this.lineDrawingCanvas.style.display == 'block') {
                        this.lineDrawingCanvas.style.display = 'none';
                        this.lineDrawer.redrawAll();
                        this.lineDrawingCanvas.style.left = '0px';
                        this.lineDrawingCanvas.style.top = '0px';
                        this.lineDrawingCanvas.style.display = 'block';
                    }

                    if (this.contourDrawingCanvas.style.display == 'block') {
                        this.contourDrawingCanvas.style.display = 'none';
                        this.contourDrawer.redrawAll();
                        this.contourDrawingCanvas.style.left = '0px';
                        this.contourDrawingCanvas.style.top = '0px';
                        this.contourDrawingCanvas.style.display = 'block';
                    }

                    if (this.img1.style.visibility == 'visible') {
                        this.img2.style.top = '0px';
                        this.img2.style.left = '0px';
                        this.img1.style.visibility = 'hidden';
                        this.img2.style.visibility = 'visible';
                    }
                    else {
                        this.img1.style.top = '0px';
                        this.img1.style.left = '0px';
                        this.img2.style.visibility = 'hidden';
                        this.img1.style.visibility = 'visible';
                    }

                }

                this.imgLoader.style.visibility = visibility;
            }

    this.callServer =
        function(arg) {
            cs = eval(this.mapId + 'CallServer');
            this.requestProcessing = true;
            this.mapImageLoading(true);
            cs(arg, '');
        }

    this.collapseLayerControl =
        function() {
            var el = this.layersControl;
            var h = parseInt(el.style.height);
            if (h > '80') {
                el.style.height = h - 80 + 'px';
                setTimeout(this.variableName + '.collapseLayerControl()', 20);
            }
            else {
                el.style.overflow = 'hidden';
                el.style.height = '25px';
            }
        }

    this.expandLayerControl =
        function() {
            var el = this.layersControl;
            var h = parseInt(el.style.height);
            if (h < '300') {
                el.style.height = h + 80 + 'px';
                setTimeout(this.variableName + '.expandLayerControl()', 20);
            }
            else {
                el.style.overflow = 'scroll';
                el.style.height = '300px';
            }
        }

    this.switchLayerControl =
                function() {
                    var el = this.layersControl;
                    if (el.style.height == '25px') {
                        this.expandLayerControl();
                    }
                    else {
                        this.collapseLayerControl();
                    }
                    return true;
                }

    this.getShapeCoordsString =
        function() {
            var coords, coordsString = '';

            if (this.selectedToolName == 'lineMeasurer') {
                coords = this.lineDrawer.Polyline;
            }
            else coords = this.contourDrawer.Polygon;


            for (var i = 0; i < coords.length; i++) {
                coordsString += coords[i].x + ';';
                coordsString += coords[i].y + ';';
            }
            return coordsString;
        }

    this.postShape =
        function() {
            var postArgument = 'post' + (this.selectedToolName ==
                'lineMeasurer' ? 'Polyline;' : 'Polygon;');

            this.measureResult.style.visibility = 'hidden';
            this.callServer(postArgument + this.getShapeCoordsString());

            if (this.selectedToolName == 'lineMeasurer')
                this.selectLineMeasurerTool();
            else this.selectAreaMeasurerTool();

            return false;
        }

    this.move =
            function(ClickedItem, e) {
                if (this.requestProcessing) return;
                if (this.cancelDrag) return;
                var ev = window.event || e;
                this.SelectedItem = ClickedItem;

                this.MouseX = DefPosition(ev).x;
                this.MouseY = DefPosition(ev).y;

                this.DraggingObjectLeft = parseInt(ClickedItem.style.left);
                this.DraggingObjectTop = parseInt(ClickedItem.style.top);
                document.onmousedown = function() { return false; };

                var varName = this.variableName;
                if (this.isIE) {
                    document.onmousemove = function() { eval(varName + '.drag()'); };
                    document.onmouseup = function() { eval(varName + '.drop()'); };
                }
                else {
                    document.onmousemove = function(e) { eval(varName + '.drag(e)'); };
                    document.onmouseup = function(e) { eval(varName + '.drop(e)'); };
                }

                if (this.SelectedItem != this.img1 && this.SelectedItem != this.img2)
                    this.setOpacity(this.SelectedItem, 0.5);
                this.oldCursor = this.SelectedItem.style.cursor;
                this.SelectedItem.style.cursor = 'default';
            }

    this.drag =
            function(e) {
                var toolname = this.selectedToolName;
                if (toolname == 'drag' || toolname == 'lineMeasurer' || toolname == 'areaMeasurer' ||
                               this.SelectedItem.id == this.objectInfo.id) {
                    var ev = window.event || e;
                    this.mapDX = DefPosition(ev).x - this.MouseX + this.DraggingObjectLeft;
                    this.mapDY = DefPosition(ev).y - this.MouseY + this.DraggingObjectTop;

                    if ((this.SelectedItem == this.img1 || this.SelectedItem == this.img2) &&
                         toolname != 'drag') {
                        this.lineDrawingCanvas.style.left = this.mapDX + 'px';
                        this.lineDrawingCanvas.style.top = this.mapDY + 'px';
                        this.contourDrawingCanvas.style.left = this.mapDX + 'px';
                        this.contourDrawingCanvas.style.top = this.mapDY + 'px';
                    }
                    this.SelectedItem.style.left = this.mapDX + 'px';
                    this.SelectedItem.style.top = this.mapDY + 'px';

                    if (Math.abs(this.mapDX) > 2 || Math.abs(this.mapDY) > 2) {
                        this.lineDrawer.processClicks = false;
                    }
                }
                return false;
            }

    this.drop =
            function(e) {
                this.SelectedItem.style.cursor = this.oldCursor;

                var ev = window.event || e;
                document.onmousemove = null;
                document.onmouseup = null;
                document.onmousedown = null;

                if (this.SelectedItem.id == this.objectInfo.id) {
                    this.setOpacity(this.SelectedItem, 0.85);
                    this.lineDrawer.processClicks = true;
                    return;
                }

                var hfXCoord = this.xValue;
                var hfYCoord = this.yValue;
                var toolname = this.selectedToolName;

                var shapeCoords = '';
                if (toolname == 'drag' || toolname == 'lineMeasurer' || toolname == 'areaMeasurer') {
                    this.xValue = this.SelectedItem.style.left;
                    this.yValue = this.SelectedItem.style.top;
                    shapeCoords = this.getShapeCoordsString();
                    this.lineDrawer.processClicks = true;
                }
                else {
                    pos = PositionOnElement(ev, this.elById(this.mapId));
                    this.xValue = pos.x;
                    this.yValue = pos.y;
                }
                this.mapDX = 0;
                this.mapDY = 0;

                if (Math.abs(parseInt(this.xValue)) > 2 || Math.abs(parseInt(this.yValue)) > 2) {
                    CallServerArgument = this.selectedToolName + ';' + this.xValue + ';' + this.yValue + ';' + shapeCoords;
                    this.callServer(CallServerArgument);
                }
            }

    this.activeMapImage =
       function() {
           if (this.img1.style.visibility == 'visible')
               return this.img1;
           else return this.img2;
       }

    this.lineDrawingCanvasMouseDown =
        function(ClickedItem, e) {
            if (!this.lineDrawer.canvasMouseDown(this, e))
                this.move(this.activeMapImage(), e);
        }

    this.contourDrawingCanvasMouseDown =
        function(ClickedItem, e) {
            if (!this.contourDrawer.canvasMouseDown(this, e))
                this.move(this.activeMapImage(), e);
        }

    this.mouseWheel =
        function(event) {
            if (document.frames)
                event = window.event;

            if (this.requestProcessing) {
                if (event.preventDefault)
                    event.preventDefault();
                return false;
            }
            var delta = 0;
            if (event.wheelDelta) {
                delta = event.wheelDelta / 120;
            }
            else if (event.detail) {
                delta = -event.detail / 3;
            }

            var shapeCoords = '';
            if (this.selectedToolName == 'lineMeasurer' || this.selectedToolName == 'areaMeasurer')
                shapeCoords = this.getShapeCoordsString();

            if (delta) {

                pos = PositionOnElement(event, this.elById(this.mapId));

                mapElem = this.elById(this.mapId);
                if (pos.x < 0 || pos.y < 0 || pos.x > parseInt(mapElem.style.width) || pos.y > parseInt(mapElem.style.height)) {
                    return true;
                }

                this.requestProcessing = true;
                if (delta > 0) {
                    this.callServer('zoomIn;' + pos.x + ';' + pos.y + ';' + shapeCoords);
                }
                else {
                    this.callServer('zoomOut;' + pos.x + ';' + pos.y + ';' + shapeCoords);
                }
            }
            if (event.preventDefault)
                event.preventDefault();
            event.returnValue = false;
        }

    this.img1 = this.elById(mapInit.id + '_img1');
    this.img2 = this.elById(mapInit.id + '_img2');

    if (this.isIE) {
        this.img1.ondrag = function(event) { return false; }
        this.img2.ondrag = function(event) { return false; }
        this.objectInfo.ondrag = function(event) { return false; }
    }

    var mouseDownStr = this.variableName + '.move(this, e);';

    //    this.img1.onmousedown =
    //        function(e) { eval(mouseDownStr) };
    //    this.img2.onmousedown =
    //        function(e) { eval(mouseDownStr) };

    this.objectInfo.onmousedown =
        function(e) { eval(mouseDownStr) };

    var onLoadStr = this.variableName + '.mapImageLoading(false);';

    this.img1.onload =
        function() { eval(onLoadStr) };
    this.img2.onload =
        function() { eval(onLoadStr) };

    var onclickStr = this.variableName + '.switchLayerControl();';

    if (this.layersSwitchButton) {
        this.layersSwitchButton.onclick =
            function() { eval(onclickStr); return false; };
    }

    this.objectInfoOpacity = 1;

    this.fadeInObjectInfo =
        function() {
            var el = this.objectInfo;
            var op = this.objectInfoOpacity;
            if (op < 0.85) {
                this.setOpacity(el, op);
                op += 0.05;
                this.objectInfoOpacity = op;
                setTimeout(this.variableName + '.fadeInObjectInfo()', 20);
            }
            else {
                this.setOpacity(el, 0.85);
            }
        }

    this.switchObjectInfo =
        function(show) {
            if (show) {
                var el = this.objectInfo;
                if (el.style.visibility == 'visible') return;
                el.style.visibility = 'visible';

                this.objectInfoOpacity = 0.1;
                this.fadeInObjectInfo();
            }
            else
                this.objectInfo.style.visibility = 'hidden';
            return false;
        }

    this.hideSelection =
        function() {
            this.switchObjectInfo(false);
            this.callServer('hideSelection')
            return false;
        }

    this.setLayersHolderContent =
            function(arg) {
                if (this.layerRecordsHolder)
                    this.layerRecordsHolder.innerHTML = arg.substring(6, arg.length);
            }

    this.changeLayerVisibility =
        function(i) {
            if (this.requestProcessing)
                return;
            this.callServer('layerVisibilityChange' + i);
            return false;
        }

    this.hideAllLayers =
        function() {
            if (this.requestProcessing)
                return;
            var el = this.layerRecordsHolder;
            for (var i = 0; i < el.childNodes.length; i++) {
                if (el.childNodes[i].checked != null)
                    el.childNodes[i].checked = false;
            }
            this.changeLayerVisibility('hideAll');
            return false;
        }

    this.showAllLayers =
        function() {
            if (this.requestProcessing) return;
            var el = this.layerRecordsHolder;
            for (var i = 0; i < el.childNodes.length; i++) {
                if (el.childNodes[i].checked != null)
                    el.childNodes[i].checked = true;
            }
            this.changeLayerVisibility('showAll');
            return false;
        }

    this.receiveCallBackResult =
        function(arg, context) {
            this.requestProcessing = false;

            // list of layers
            if (arg.substring(0, 6) == "layers") {
                if (this.layerControlEnabled)
                    this.setLayersHolderContent(arg);

                return true;
            }

            // Information about
            var k = 0;
            if (arg.substring(0, 10) == 'objectInfo') {
                var i = 10;
                while ('0123456789'.indexOf(arg.charAt(i)) != -1) {
                    i++;
                }
                k = parseInt(arg.substring(10, i)) + i;
                this.infoStr = decodeURI(arg.substring(i, k));

                this.info.innerHTML = this.infoStr;

                if (this.selectedToolName == 'info')
                    this.switchObjectInfo(this.infoStr != '');
            }

            k++;
            i = k;

            // pixel size
            while ("0123456789.eE-".indexOf(arg.charAt(i)) != -1)
                i++;
            var ps = parseFloat(arg.substring(k, i));
            this.lineDrawer.pixelSize = ps;
            this.contourDrawer.pixelSize = ps;
            i++; k = i;

            // scale data segment
            if (arg.substring(k, k + 9) == 'scaleData') {
                k += 9;
                i = k;
                while ('0123456789'.indexOf(arg.charAt(i)) != -1)
                    i++;
                var f = parseInt(arg.substring(k, i));
                k = f + i;
                if (f > 0) {
                    if (this.scaleLabel)
                        this.scaleLabel.innerHTML = arg.substring(i, k);

                    i = k;
                    while ('0123456789'.indexOf(arg.charAt(i)) != -1)
                        i++;

                    if (this.scaleSegment)
                        this.scaleSegment.style.width = parseInt(arg.substring(k, i)) + 'px';

                    k = i + 1;
                }
            }

            // mode
            if (arg.substring(k, k + 4) == 'mode') {
                k += 4;
                i = k;
                while (arg.charAt(i) != ' ')
                    i++;
                this.switchTo(arg.substring(k, i));
                k = i + 1;
            }

            // coordinates of editable shapes
            if (arg.substring(k, k + 11) == 'shapeCoords') {
                coordsArray = new Array();
                k += 11;
                i = k;
                while ('0123456789'.indexOf(arg.charAt(i)) != -1)
                    i++;
                f = parseInt(arg.substring(k, i));
                k = i + 1;
                for (var q = 0; q < f; q++) {
                    i = k;
                    while ('-0123456789.'.indexOf(arg.charAt(i)) != -1)
                        i++;
                    var x = parseFloat(arg.substring(k, i));
                    k = i + 1;

                    i = k;
                    while ('-0123456789.'.indexOf(arg.charAt(i)) != -1)
                        i++;
                    var y = parseFloat(arg.substring(k, i));
                    k = i + 1;
                    coordsArray[q] = { x: x, y: y };
                }
                this.lineDrawer.Polyline = coordsArray;
                this.contourDrawer.Polygon = coordsArray;
            }

            //  map image
            if (this.img1.style.visibility == 'visible') {
                this.img2.src = arg.substring(k, arg.length);
            }
            else
                this.img1.src = arg.substring(k, arg.length);
        }

    if (this.layerControlEnabled)
        this.callServer('layerListQuery');
}

//-----------------------------------------------
// distance meter
//-----------------------------------------------
var distanceMeasurer = function(p) {
    this.pointColor = p.pointColor;
    this.pointRadius = p.pointRadius;

    this.alwaysDisplayLabel = p.alwaysDisplayLabel;

    this.lineColor = p.lineColor;
    this.lineWidth = p.lineWidth;

    this.shadowColor = p.shadowColor;
    this.shadowOffset = p.shadowOffset;

    this.canvasWidth = p.canvasWidth;
    this.canvasHeight = p.canvasHeight;

    this.Polyline = new Array();

    this.selectedPointIndex = -1;
    this.pixelSize = p.pixelSize;

    this.doc = p.doc;
    this.canvasId = p.canvasId;

    this.divId = p.divId;
    this.metric = p.metric;

    this.isIE = navigator.appName == "Microsoft Internet Explorer";

    this.lastMouseUpTime = new Date();
    this.mouseDownPoint = { x: 0, y: 0 };

    this.processClicks = true;

    this.getCanvas = function() {
        return this.doc.getElementById(this.canvasId);
    }

    this.distance =
        function(p1, p2) {
            var dx = p1.x - p2.x;
            var dy = p1.y - p2.y;
            return Math.sqrt(dx * dx + dy * dy);
        }

    this.drawPoint =
        function(ctx, x, y, isEndPoint) {
            ctx.fillStyle = this.shadowColor;
            ctx.lineWidth = 5;
            ctx.beginPath();
            ctx.arc(x + this.shadowOffset, y + this.shadowOffset, this.pointRadius, 0, Math.PI * 2, true);
            ctx.fill();

            ctx.fillStyle = this.pointColor;
            ctx.beginPath();
            ctx.arc(x, y, this.pointRadius, 0, Math.PI * 2, true);
            ctx.fill();

            if (isEndPoint) {
                ctx.fillStyle = 'rgba(255, 255, 255, 0.5)';
                ctx.beginPath();
                ctx.arc(x, y, this.pointRadius / 2, 0, Math.PI * 2, true);
                ctx.fill();
            }
        }

    this.drawLine =
        function(ctx, x1, y1, x2, y2) {
            ctx.strokeStyle = this.shadowColor;
            ctx.lineWidth = this.lineWidth;
            ctx.beginPath();
            ctx.moveTo(x1 + this.shadowOffset, y1 + this.shadowOffset);
            ctx.lineTo(x2 + this.shadowOffset, y2 + this.shadowOffset);
            ctx.stroke();

            ctx.strokeStyle = this.lineColor;
            ctx.beginPath();
            ctx.moveTo(x1, y1);
            ctx.lineTo(x2, y2);
            ctx.stroke();
        }

    this.showLabel =
        function(visible, x, y, labelText) {
            if (this.alwaysDisplayLabel && !visible)
                return;

            var infoDiv = this.doc.getElementById(this.divId);
            infoDiv.style.top = y - 40 + 'px';
            infoDiv.style.left = x + 5 + 'px';
            if (visible && labelText != '')
                infoDiv.style.visibility = 'visible';
            else infoDiv.style.visibility = 'hidden';

            var s = labelText + this.metric;

            if (typeof infoDiv.firstChild.innerText != 'undefined')
                infoDiv.firstChild.innerText = s;
            else
                infoDiv.firstChild.textContent = s;
        }

    this.drawPolyline =
        function drawPolyline(ctx, polyline) {
            ctx.fillStyle = 'rgba(0, 0, 0, 0)';
            ctx.fillRect(0, 0, this.canvasWidth, this.canvasHeight);
            for (var i = 0; i < polyline.length - 1; i++)
                this.drawLine(ctx, polyline[i].x, polyline[i].y, polyline[i + 1].x, polyline[i + 1].y);

            for (i = 0; i < polyline.length - 1; i++)
                this.drawPoint(ctx, polyline[i].x, polyline[i].y, false);

            if (polyline.length > 0)
                this.drawPoint(ctx, polyline[i].x, polyline[i].y, true);
        }

    this.redrawAll =
        function() {
            var ctx = this.getCanvas().getContext('2d');
            ctx.clearRect(0, 0, this.canvasWidth, this.canvasHeight);
            this.drawPolyline(ctx, this.Polyline)
            var visible = false;
            var x = y = 0;

            if (typeof this.Polyline != 'undefined') {
                visible = this.Polyline.length > 1;
                if (visible) {
                    x = this.Polyline[this.Polyline.length - 1].x;
                    y = this.Polyline[this.Polyline.length - 1].y;
                }
            }

            this.showLabel(visible, x, y, '');
        }

    this.canvasMouseDown =
        function(clickedItem, e) {
            var ev = window.event || e;
            var p = PositionOnElement(ev, this.getCanvas());
            var result = false;
            this.mouseDownPoint = p;

            for (i = 0; i < this.Polyline.length; i++) {
                if (this.distance(this.Polyline[i], p) < this.pointRadius) {
                    this.selectedPointIndex = i;
                    result = true;
                }
            }

            if (ev.preventDefault)
                ev.preventDefault();
            ev.returnValue = false;

            return result || this.Polyline.length == 0;
        }

    this.canvasMouseMove =
        function(clickedItem, e) {
            var ev = window.event || e;
            var p = PositionOnElement(ev, this.getCanvas());

            if (ev.preventDefault)
                ev.preventDefault();
            ev.returnValue = false;

            var inCursorIndex = -1;

            if (this.selectedPointIndex != -1) {
                if (this.isIE) {
                    if (ev.button != 1) {
                        this.selectedPointIndex = -1;
                        return;
                    }
                }
                this.Polyline[this.selectedPointIndex] = p;
                this.redrawAll();
            }

            for (i = 0; i < this.Polyline.length; i++) {
                if (this.distance(this.Polyline[i], p) < this.pointRadius)
                    inCursorIndex = i;
            }
            if (inCursorIndex != -1 && this.Polyline.length > 1) {
                var dist = 0;
                for (i = 0; i < inCursorIndex; i++) dist += this.distance(this.Polyline[i], this.Polyline[i + 1]);
                var labelText = (dist * this.pixelSize).toFixed(2).toString();
                this.showLabel(true, this.Polyline[inCursorIndex].x, this.Polyline[inCursorIndex].y, labelText);
            }
            else this.showLabel(false, 0, 0, '');
        }

    this.canvasMouseUp =
        function(clickedItem, e) {
            if (!this.processClicks)
                return;

            var d = new Date();
            var timeBetweenClicks = d - this.lastMouseUpTime;

            this.lastMouseUpTime = d;

            var ev = window.event || e;
            var p = PositionOnElement(ev, this.getCanvas());

            if (this.selectedPointIndex == -1 && this.Polyline.length > 0 &&
                (Math.abs(this.mouseDownPoint.x - p.x) > 2 ||
                 Math.abs(this.mouseDownPoint.y - p.y) > 2))
                return;

            if (timeBetweenClicks < 300) {
                for (i = 0; i < this.Polyline.length; i++)
                    if (this.distance(this.Polyline[i], p) < this.pointRadius) {
                    this.Polyline.splice(i, 1);
                    this.redrawAll();
                }
                this.selectedPointIndex = -1;
                return;
            }

            if (this.selectedPointIndex == -1) {
                this.Polyline[this.Polyline.length] = p;
                this.redrawAll();
            }
            this.selectedPointIndex = -1;
        }
}

//-----------------------------------------------
// meter area
//-----------------------------------------------
var areaMeasurer = function(p) {
    this.pointColor = p.pointColor;
    this.pointRadius = p.pointRadius;

    this.alwaysDisplayLabel = p.alwaysDisplayLabel;

    this.lineColor = p.lineColor;
    this.lineWidth = p.lineWidth;

    this.shadowColor = p.shadowColor;
    this.shadowOffset = p.shadowOffset;

    this.fillColor = p.fillColor;

    this.canvasWidth = p.canvasWidth;
    this.canvasHeight = p.canvasHeight;

    this.Polygon = new Array();

    this.selectedPointIndex = -1;
    this.pixelSize = p.pixelSize;

    this.doc = p.doc;
    this.canvasId = p.canvasId;

    this.divId = p.divId;
    this.metric = p.metric;

    this.isIE = navigator.appName == "Microsoft Internet Explorer";

    this.lastMouseUpTime = new Date();

    this.getCanvas = function() {
        return this.doc.getElementById(this.canvasId);

    }

    this.distance =
    function(p1, p2) {
        var dx = p1.x - p2.x;
        var dy = p1.y - p2.y;
        return Math.sqrt(dx * dx + dy * dy);
    }

    this.centerPoint =
    function(p1, p2) {
        return {
            x: (p1.x + p2.x) / 2,
            y: (p1.y + p2.y) / 2
        }
    }


    this.drawPoint =
    function(ctx, x, y, isBridgePoint) {
        ctx.fillStyle = this.shadowColor;

        var pr = this.pointRadius;

        ctx.beginPath();
        ctx.arc(x + this.shadowOffset, y + this.shadowOffset, pr, 0, Math.PI * 2, true);
        ctx.fill();

        ctx.fillStyle = this.pointColor;
        ctx.beginPath();
        ctx.arc(x, y, pr, 0, Math.PI * 2, true);
        ctx.fill();

        if (isBridgePoint) {
            ctx.fillStyle = 'rgba(255, 255, 255, 0.3)';
            ctx.beginPath();
            ctx.arc(x, y, pr, 0, Math.PI * 2, true);
            ctx.fill();
        }
    }

    this.drawLine =
    function(ctx, x1, y1, x2, y2) {
        ctx.strokeStyle = this.shadowColor;
        ctx.lineWidth = this.lineWidth;
        ctx.beginPath();
        ctx.moveTo(x1 + this.shadowOffset, y1 + this.shadowOffset);
        ctx.lineTo(x2 + this.shadowOffset, y2 + this.shadowOffset);
        ctx.stroke();

        ctx.strokeStyle = this.lineColor;
        ctx.beginPath();
        ctx.moveTo(x1, y1);
        ctx.lineTo(x2, y2);
        ctx.stroke();
    }

    this.fullArea =
    function() {
        var i, j, hp;
        var result = 0;
        hp = this.Polygon.length;
        for (i = 0; i < hp; i++) {
            if (i == hp - 1) j = 0;
            else j = i + 1;
            result += (this.Polygon[i].x + this.Polygon[j].x) * (this.Polygon[i].y - this.Polygon[j].y);
        }
        return Math.abs(result) / 2 * this.pixelSize * this.pixelSize;
    }

    this.showLabel =
        function(visible, x, y, labelText) {

            if (this.alwaysDisplayLabel && !visible)
                return;

            var infoDiv = this.doc.getElementById(this.divId);
            infoDiv.style.top = y - 40 + 'px';
            infoDiv.style.left = x + 5 + 'px';
            if (visible && labelText != '')
                infoDiv.style.visibility = 'visible';
            else infoDiv.style.visibility = 'hidden';

            var s = labelText + this.metric;

            if (typeof infoDiv.firstChild.innerText != 'undefined')
                infoDiv.firstChild.innerText = s;
            else
                infoDiv.firstChild.textContent = s;
        }

    this.drawPolygon =
    function drawPolyline(ctx, polygon) {
        ctx.fillStyle = 'rgba(0, 0, 0, 0)';
        ctx.fillRect(0, 0, this.canvasWidth, this.canvasHeight);

        ctx.fillStyle = this.fillColor;
        ctx.beginPath();
        if (polygon.length > 0)
            ctx.moveTo(polygon[0].x, polygon[0].y);
        for (var i = 0; i < polygon.length - 1; i++)
            ctx.lineTo(polygon[i + 1].x, polygon[i + 1].y);

        if (polygon.length > 0)
            ctx.lineTo(polygon[0].x, polygon[0].y);
        ctx.fill();

        for (i = 0; i < polygon.length - 1; i++) {
            this.drawLine(ctx, polygon[i].x, polygon[i].y, polygon[i + 1].x, polygon[i + 1].y);
            var p = this.centerPoint(polygon[i], polygon[i + 1]);
            this.drawPoint(ctx, p.x, p.y, true);
        }

        if (polygon.length > 0) {
            this.drawLine(ctx, polygon[i].x, polygon[i].y, polygon[0].x, polygon[0].y);
            p = this.centerPoint(polygon[i], polygon[0]);
            this.drawPoint(ctx, p.x, p.y, true);
        }

        for (i = 0; i < polygon.length; i++)
            this.drawPoint(ctx, polygon[i].x, polygon[i].y, false);
    }

    this.redrawAll =
    function() {
        var ctx = this.doc.getElementById(this.canvasId).getContext('2d');

        ctx.clearRect(0, 0, this.canvasWidth, this.canvasHeight);
        this.drawPolygon(ctx, this.Polygon)
        var visible = false;
        var x = y = 0;

        if (typeof this.Polygon != 'undefined') {
            visible = this.Polygon.length > 1;
            if (visible) {
                x = this.Polygon[this.Polygon.length - 1].x;
                y = this.Polygon[this.Polygon.length - 1].y;
            }
        }

        this.showLabel(visible, x, y, '');
    }

    this.segmentsIntercect =
    function(s1V1, s1V2, s2V1, s2V2) {
        var u1Numerator = (s2V2.x - s2V1.x) * (s1V1.y - s2V1.y) - (s2V2.y - s2V1.y) * (s1V1.x - s2V1.x);
        var u2Numerator = (s1V2.x - s1V1.x) * (s1V1.y - s2V1.y) - (s1V2.y - s1V1.y) * (s1V1.x - s2V1.x);
        var Denominator = (s2V2.y - s2V1.y) * (s1V2.x - s1V1.x) - (s2V2.x - s2V1.x) * (s1V2.y - s1V1.y);

        if (Denominator == 0) return false;

        var u1 = u1Numerator / Denominator;
        var u2 = u2Numerator / Denominator;
        if (u1 >= 0 && u1 <= 1 && u2 >= 0 && u2 <= 1) return true;
        else return false;
    }

    this.hasIntercections =
    function() {
        var begin1, end1, begin2, end2;
        for (i = 0; i < this.Polygon.length - 1; i++) {
            for (j = i + 2; j < this.Polygon.length - 1; j++) {
                if (this.segmentsIntercect(this.Polygon[i], this.Polygon[i + 1], this.Polygon[j], this.Polygon[j + 1])) return true;
            }
        }
        i = this.Polygon.length - 1;
        if (i > 1) {
            for (j = 1; j < this.Polygon.length - 2; j++) {
                if (this.segmentsIntercect(this.Polygon[i], this.Polygon[0], this.Polygon[j], this.Polygon[j + 1])) return true;
            }
        }
        return false;
    }

    this.canvasMouseDown =
    function(clickedItem, e) {
        var ev = window.event || e;
        var p = PositionOnElement(ev, this.getCanvas()); ;

        if (ev.preventDefault)
            ev.preventDefault();
        ev.returnValue = false;

        for (i = 0; i < this.Polygon.length; i++) {
            if (this.distance(this.Polygon[i], p) < this.pointRadius) {
                this.selectedPointIndex = i;
                return true;
            }
        }

        for (i = 0; i < this.Polygon.length; i++) {
            var j = i < this.Polygon.length - 1 ? (i + 1) : 0;
            var p1 = this.centerPoint(this.Polygon[i], this.Polygon[j]);
            if (this.distance(p, p1) < this.pointRadius) {
                this.Polygon.splice(j, 0, p);
                this.selectedPointIndex = j;
                this.redrawAll();
                return true;
            }
        }

        return this.Polygon.length == 0;
    }

    this.canvasMouseMove =
    function(clickedItem, e) {
        var ev = window.event || e;
        var p = PositionOnElement(ev, this.getCanvas());

        if (ev.preventDefault)
            ev.preventDefault();
        ev.returnValue = false;

        var inCursorIndex = -1;

        if (this.selectedPointIndex != -1) {
            if (this.isIE) {
                if (ev.button != 1) {
                    this.selectedPointIndex = -1;
                    return;
                }
            }
            this.Polygon[this.selectedPointIndex] = p;
            this.redrawAll();
        }

        for (i = 0; i < this.Polygon.length; i++) {
            if (this.distance(this.Polygon[i], p) < this.pointRadius)
                inCursorIndex = i;
        }
        if (inCursorIndex != -1 && this.Polygon.length > 1) {
            if (!this.hasIntercections()) {
                var dist = 0;
                var labelText = this.fullArea().toFixed(2).toString();
                this.showLabel(true, this.Polygon[inCursorIndex].x, this.Polygon[inCursorIndex].y, labelText);
            }
            else this.showLabel(true, this.Polygon[inCursorIndex].x, this.Polygon[inCursorIndex].y, "------");
        }
        else this.showLabel(false, 0, 0, '');
    }

    this.canvasMouseUp =
    function(clickedItem, e) {
        var d = new Date();
        var timeBetweenClicks = d - this.lastMouseUpTime;

        this.lastMouseUpTime = d;

        var ev = window.event || e;
        var p = PositionOnElement(ev, this.getCanvas());

        if (timeBetweenClicks < 300) {
            if (this.Polygon.length > 3) {
                for (i = 0; i < this.Polygon.length; i++)
                    if (this.distance(this.Polygon[i], p) < this.pointRadius) {
                    this.Polygon.splice(i, 1);
                    this.redrawAll();
                }
            }
            this.selectedPointIndex = -1;
            return;
        }

        if (this.selectedPointIndex == -1) {
            if (this.Polygon.length == 0) {
                this.Polygon[this.Polygon.length] = p;
                this.Polygon[this.Polygon.length] = { x: p.x + 35, y: p.y };
                this.Polygon[this.Polygon.length] = { x: p.x + 35, y: p.y + 35 };
                this.Polygon[this.Polygon.length] = { x: p.x, y: p.y + 35 };
                this.redrawAll();
            }
            else {
                //this.Polygon[this.Polygon.length] = p;
                //this.redrawAll();
            }
        }
        this.selectedPointIndex = -1;
    }
}

