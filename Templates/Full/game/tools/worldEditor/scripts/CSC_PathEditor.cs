//-----------------------------------------------------------------------------
// Copyright (c) 2018 Twisted Jenius LLC
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------


//This function updates all controls in the editor to be equal to that of the selected path
function CSC_Editor::updatePathControls(%this)
{
	%data = CSC_PathEditorSelector.getText();

    if (!isObject(%data))
        return;

	if (%data.getName() !$= "")
		CSC_PathNameSelector.text = %data.getName();
	else
    {
        if ($pathMarkerCount < 10)
           CSC_PathNameSelector.text = "Path0" @ $pathMarkerCount;
        else
           CSC_PathNameSelector.text = "Path" @ $pathMarkerCount;
    }

	if (%data.getGroup().getName() !$= "")
		CSC_SimGroupSelector2.setText(getField(%data.getGroup().getName(), 0));
	else
		CSC_SimGroupSelector2.setText("MissionGroup");

	if (%data.isLooping $= "1")
  		CSC_LoopingSelector.setValue("1");
	else
  		CSC_LoopingSelector.setValue("0");

	if (%data.isPingPong $= "1")
  		CSC_PingPongSelector.setValue("1");
	else
  		CSC_PingPongSelector.setValue("0");

	if (%data.loopCount !$= "")
		CSC_LoopCountSelector.setText(%data.loopCount);
	else
		CSC_LoopCountSelector.setText("0");

	if (%data.nextPath !$= "")
		CSC_NextPathSelector.setSelected(CSC_NextPathSelector.findText(%data.nextPath));
	else
		CSC_NextPathSelector.setSelected(0);

	if (%data.data !$= "")
		CSC_DatablockSelector.setSelected(CSC_DatablockSelector.findText(%data.data));
	else
		CSC_DatablockSelector.setSelected(CSC_DatablockSelector.findText($CSC_Datablock));

	if (%data.callFunction !$= "")
		CSC_FunctionSelector.setText(%data.callFunction);
	else
		CSC_FunctionSelector.setText("");

    %count = %data.getCount();

    if (%count > 0)
    {
        //Values that are based on the first node
        %node = %data.getObject(0);

        if (%node.timeScale !$= "")
	        CSC_TimeScaleSelector.setText(%node.timeScale);
        else
	        CSC_TimeScaleSelector.setText($timeScale);

        if (%node.fade > 0)
	        CSC_FadeSelector.setText(%node.fade);
        else
	        CSC_FadeSelector.setText("0");

        if (%node.black > 0)
	        CSC_BlackSelector.setText(%node.black);
        else
	        CSC_BlackSelector.setText("0");

        if (%node.color > 0)
	        CSC_FadeColor1.color = %node.color;
        else
	        CSC_FadeColor1.color = "0 0 0 1";

        if (%node.mode !$= "")
	        CSC_FocalModeSelector.setSelected(CSC_FocalModeSelector.findText(%node.mode));
        else
	        CSC_FocalModeSelector.setSelected(0);

        if (%node.axis !$= "")
	        CSC_AxisSelector.setSelected(%node.axis);
        else
	        CSC_AxisSelector.setSelected(0);

        if (%node.degree !$= "")
	        CSC_RotationSelector.setText(%node.degree);
        else
	        CSC_RotationSelector.setText("0");

        if (%node.location !$= "")
	        CSC_LocationSelector.setText(%node.location);
        else
	        CSC_LocationSelector.setText("0 0 0");

        if (%node.offset !$= "")
	        CSC_OffsetSelector.setText(%node.offset);
        else
	        CSC_OffsetSelector.setText("0 0 0");

        if (%node.object $= "allBotsGroup.getObject(0)")
            CSC_ObjectSelector.setSelected(CSC_ObjectSelector.findText("-UAISK BOT ONE"));
        else if (%node.object $= "LocalClientConnection.player")
            CSC_ObjectSelector.setSelected(CSC_ObjectSelector.findText("-PLAYER (LOCAL)"));
        else if (%node.object !$= "")
	        CSC_ObjectSelector.setSelected(CSC_ObjectSelector.findText(%node.object));
        else
	        CSC_ObjectSelector.setSelected(0);

        if (%node.zoom !$= "")
	        CSC_ZoomSelector.setText(%node.zoom);
        else
	        CSC_ZoomSelector.setText($pref::Player::zoomSpeed);

        //Values that are based on the last node
        if (%count > 1)
            %last = %data.getObject(%count - 1);

        if (%last.fade > 0)
	        CSC_FinalFadeSelector.setText(%last.fade);
        else
	        CSC_FinalFadeSelector.setText("0");

        if (%last.black > 0)
	        CSC_FinalBlackSelector.setText(%last.black);
        else
	        CSC_FinalBlackSelector.setText("0");

        if (%last.color > 0)
	        CSC_FadeColor2.color = %last.color;
        else
	        CSC_FadeColor2.color = "0 0 0 1";
    }
    else
    {
        CSC_TimeScaleSelector.setText($timeScale);
        CSC_FadeSelector.setText("0");
        CSC_BlackSelector.setText("0");
        CSC_FocalModeSelector.setSelected(0);
        CSC_AxisSelector.setSelected(0);
        CSC_AxisSelector.setText("0");
        CSC_LocationSelector.setText("0 0 0");
        CSC_OffsetSelector.setText("0 0 0");
        CSC_ObjectSelector.setSelected(0);
        CSC_ZoomSelector.setText($pref::Player::zoomSpeed);
        CSC_FinalFadeSelector.setText("0");
        CSC_FinalBlackSelector.setText("0");
        CSC_FadeColor1.color("0 0 0 1");
        CSC_FadeColor2.color("0 0 0 1");
    }

    CSC_Editor::editorSelectObject(%data);

	echo("Selected Path: " @ %data);
}

//This function applies any changes made to the path
function CSC_Editor::savePathEffect(%this, %mode)
{
    CSC_Editor::editorDeselectObject();

	%data = CSC_PathEditorSelector.getText();

    //Set the path's values if we're editing
    if (%mode $= "edit")
    {
	    if (CSC_PathNameSelector.getText() $= "")
        {
            if ($pathMarkerCount < 10)
               CSC_PathNameSelector.text = "Path0" @ $pathMarkerCount;
            else
               CSC_PathNameSelector.text = "Path" @ $pathMarkerCount;
        }

        %obj = %data.getid();

        %obj.setName(CSC_PathNameSelector.getText());
        %obj.loopCount = CSC_LoopCountSelector.getText();
        %obj.isLooping = CSC_LoopingSelector.getValue();

        if (%obj.isLooping <= 0)
            %obj.isPingPong = CSC_PingPongSelector.getValue();
        else
            %obj.isPingPong = "";

        if (isObject(CSC_NextPathSelector.getText()) && CSC_NextPathSelector.getText() !$= %obj.getName())
            %obj.nextPath = CSC_NextPathSelector.getText();
        else
            %obj.nextPath = "";

        if (CSC_DatablockSelector.getText() !$= $CSC_Datablock)
            %obj.data = CSC_DatablockSelector.getText();
        else
            %obj.data = "";

        %obj.callFunction = CSC_FunctionSelector.getText();

	    //Put it in the correct simgroup
	    %simGroupSet = CSC_SimGroupSelector2.getText();

        if (getField(%obj.getGroup().getName(), 0) !$= %simGroupSet)
	        %simGroupSet.add(%obj);

        %count = CSC_PathNameSelector.getText().getCount();

        //Save some values to the first node of the path as well
        if (%count > 0)
            %this.saveFirstNode(CSC_PathNameSelector.getText().getObject(0));
        if (%count > 1)
            %this.saveLastNode(CSC_PathNameSelector.getText().getObject(%count - 1));

        //Update all lists
        CSC_Editor.initEditor();

        %order = CSC_PathEditorSelector.findText(%obj.getName());

        if ($pathMarkerCount > 1)
        {
            //Should the next path we select be above or below the last one
            if ($CSC_SelectNextMarker == 0)
            {
                if (%order < $pathMarkerCount - 2)
                    %order++;
                else
                    %order = 0;
            }
            else if ($CSC_SelectNextMarker == 1)
            {
                if (%order > 0)
                    %order--;
                else
                    %order = $pathMarkerCount - 2;
            }
        }

        CSC_PathEditorSelector.setText(CSC_PathEditorSelector.getTextById(%order));
        CSC_PathEditorSelector2.setText(CSC_PathEditorSelector2.getTextById(%order));

        CSC_Editor.updatePathControls();
        CSC_Editor.populateNodelist();
    }
    //Make a new path
    else
    {
        if ($pathMarkerCount < 10)
           %name = "Path0" @ $pathMarkerCount;
        else
           %name = "Path" @ $pathMarkerCount;

        if (isObject(%name))
            %name = "Path" @ getRandom(1, 999);

        //Setup the objects data
        %marker = new Path(%name) {
            canSaveDynamicFields = "1";
            isLooping = "0";
        };

	    //Save it in the correct simgroup
	    %simGroupSet = CSC_SimGroupSelector2.getText();
	    %simGroupSet.add(%marker);

        //Update all lists
        CSC_Editor.initEditor();
        CSC_Editor.updatePathControls();

        CSC_PathEditorSelector.setSelected(CSC_PathEditorSelector.findText(%name));
    }
}

//Populate the nodes list based on the selected path
function CSC_Editor::populateNodelist(%this)
{
    CSC_NodeSelector.clear();
    CSC_NodeSelector2.clear();

    %path = CSC_PathEditorSelector.getText();

    if (!isObject(%path))
        return;

    %count = %path.getCount();

    //Cycle through all nodes on this path and add them to the node list
    for (%i = 0; %i < %count; %i++)
    {
        %nodeName = %path.getObject(%i).getName();
        CSC_NodeSelector.add(%nodeName, %i);
        CSC_NodeSelector2.add(%nodeName, %i);
    }

    CSC_NodeSelector.setSelected(0);
    CSC_NodeSelector2.setSelected(0);
    CSC_Editor.updateNodeControls();
}

//This function updates all controls in the editor to be equal to that of the selected node
function CSC_Editor::updateNodeControls(%this, %selectorId)
{
    if (%selectorId)
	    %data = CSC_NodeSelector2.getText();
    else
	    %data = CSC_NodeSelector.getText();

	if (!isObject(%data))
        return;

	if (%data.getName() !$= "")
		CSC_NodeNameSelector.text = %data.getName();
	else
		CSC_NodeNameSelector.text = "Node" @ getRandom(1, 9999);

	if (%data.position !$= "")
		CSC_PositionSelector2.text = %data.getFieldValue(position);
	else
		CSC_PositionSelector2.text = "0 0 0";

	CSC_SeqNumSelector.setText(getField(%data.seqNum, 0));
	CSC_MsToNextSelector.setText(getField(%data.msToNext, 0));

	if (%data.fov !$= "")
		CSC_FovSelector.text = %data.fov;
	else
		CSC_FovSelector.text = $pref::Player::defaultFov;

	if (%data.shake !$= "")
		CSC_ShakeSelector.text = %data.shake;
	else
		CSC_ShakeSelector.text = "0";

	if (%data.speed !$= "")
		CSC_SpeedSelector.text = %data.speed;
	else
		CSC_SpeedSelector.text = $CSC_Speed;

    if (%data.type !$= "")
        CSC_TypeSelector.setSelected(CSC_TypeSelector.findText(%data.type));
    else
        CSC_TypeSelector.setSelected(0);

    if (%data.smoothingType !$= "")
        CSC_SmoothingTypeSelector.setSelected(CSC_SmoothingTypeSelector.findText(%data.smoothingType));
    else
        CSC_SmoothingTypeSelector.setSelected(0);

    CSC_Editor::editorSelectObject(%data);

	echo("Selected Node: " @ %data);
}

//This function applies any changes made to the node
function CSC_Editor::saveNodeEffect(%this, %mode)
{
    %path = CSC_PathEditorSelector.getText();

    CSC_Editor::editorDeselectObject();

	//Make sure the path has a name
	if (CSC_NodeNameSelector.getText() $= "")
		CSC_NodeNameSelector.text = "Node" @ getRandom(1, 9999);

	%data = CSC_NodeSelector.getText();
    %data3 = CSC_NodeNameSelector.getText();

    //Set the node's values if we're editing
    if (%mode $= "edit")
    {
        %obj = %data.getid();

        %obj.setName(%data3);
        %obj.position = CSC_PositionSelector2.getText();
        %obj.seqNum = CSC_SeqNumSelector.getText();
        %obj.msToNext = CSC_MsToNextSelector.getText();

        %fov = CSC_FovSelector.getText();

        if (%fov >= 1 && %fov <= 179 && %fov != $pref::Player::defaultFov)
            %obj.fov = %fov;
        else
            %obj.fov = "";

        if (CSC_ShakeSelector.getText() > 0)
            %obj.shake = CSC_ShakeSelector.getText();
        else
            %obj.shake = "";

        %speed = CSC_SpeedSelector.getText();

        if (%speed > 0 && %speed != $CSC_Speed)
            %obj.speed = %speed;
        else
            %obj.speed = "";

        if (CSC_ShakeSelector.getText() > 0)
            %obj.shake = CSC_ShakeSelector.getText();
        else
            %obj.shake = "";

        %obj.type = CSC_TypeSelector.getText();
        %obj.smoothingType = CSC_SmoothingTypeSelector.getText();

        //Update all lists
        CSC_Editor.initEditor();
        CSC_PathEditorSelector.setSelected(CSC_PathEditorSelector.findText(%path));

        %order = CSC_NodeSelector.findText(%obj.getName());

        //Should the next node we select be above or below the last one
        if ($CSC_SelectNextMarker == 0)
        {
            if (%order < %path.getCount() - 1)
                %order++;
            else
                %order = 0;
        }
        else if ($CSC_SelectNextMarker == 1)
        {
            if (%order > 0)
                %order--;
            else
                %order = %path.getCount() - 1;
        }

        CSC_NodeSelector.setText(CSC_NodeSelector.getTextById(%order));
        CSC_Editor.updateNodeControls();
    }
    //Make a new node
    else
    {
        //If a path hasn't been made yet, make one first
        if (!isObject(%path))
        {
            CSC_Editor.savePathEffect("create");
            CSC_Editor.saveNodeEffect("create");
            return;
        }

        if (isObject(%data3))
            CSC_NodeNameSelector.text = "Node" @ getRandom(1, 9999);

        CSC_Editor.setNodePositioning();

        //Setup the objects data
        %marker = new Marker(CSC_NodeNameSelector.getText()) {
            canSaveDynamicFields = "1";
            rotation = "1 0 0 0";
            scale = "1 1 1";
            type = "Normal";
            smoothingType = "Spline";
            position = CSC_PositionSelector2.getText();
            seqNum = "1";
            msToNext = "0";
        };

	    //Save it in the correct path
	    %simGroupSet = CSC_PathEditorSelector.getText();
	    %simGroupSet.add(%marker);

        //Update all lists
        CSC_Editor.initEditor();
        CSC_Editor.updatePathControls();

        CSC_PathEditorSelector.setSelected(CSC_PathEditorSelector.findText(%path));
    }
}

//Only save this stuff to the first node
function CSC_Editor::saveFirstNode(%this, %node)
{
    CSC_Editor::editorDeselectObject();

    if (CSC_TimeScaleSelector.getText() > 0 && CSC_TimeScaleSelector.getText() != $timeScale)
        %node.timeScale = CSC_TimeScaleSelector.getText();
    else
        %node.timeScale = "";

    if (CSC_FadeSelector.getText() > 0)
        %node.fade = CSC_FadeSelector.getText();
    else
        %node.fade = "";

    if (CSC_BlackSelector.getText() > 0)
        %node.black = CSC_BlackSelector.getText();
    else
        %node.black = "";

    if (CSC_FadeColor1.color !$= "0 0 0 1")
        %node.color = CSC_FadeColor1.color;
    else
        %node.color = "";

    if (CSC_FocalModeSelector.getText() !$= "")
        %node.mode = CSC_FocalModeSelector.getText();
    else
        %node.mode = "";

    %node.axis = CSC_AxisSelector.getSelected();

    if (CSC_RotationSelector.getText() >= -360 && CSC_RotationSelector.getText() <= 360)
        %node.degree = CSC_RotationSelector.getText();
    else
        %node.degree = "";

    if (CSC_LocationSelector.getText() !$= "")
        %node.location = CSC_LocationSelector.getText();
    else
        %node.location = "";

    if (CSC_OffsetSelector.getText() !$= "")
        %node.offset = CSC_OffsetSelector.getText();
    else
        %node.offset = "";

    %obj = CSC_ObjectSelector.getText();

    if (isObject(%obj))
        %node.object = %obj;
    else if (%obj $= "-PLAYER (LOCAL)")
        %node.object = "LocalClientConnection.player";
    else if (%obj $= "-UAISK BOT ONE")
        %node.object = "allBotsGroup.getObject(0)";
    else
        %node.object = "";

    if (CSC_ZoomSelector.getText() > 0 && CSC_ZoomSelector.getText() != $pref::Player::zoomSpeed)
        %node.zoom = CSC_ZoomSelector.getText();
    else
        %node.zoom = "";
}

//Only save this stuff to the last node
function CSC_Editor::saveLastNode(%this, %node)
{
    CSC_Editor::editorDeselectObject();

    if (CSC_FinalFadeSelector.getText() > 0)
        %node.fade = CSC_FinalFadeSelector.getText();
    else
        %node.fade = "";

    if (CSC_FinalBlackSelector.getText() > 0)
        %node.black = CSC_FinalBlackSelector.getText();
    else
        %node.black = "";

    if (CSC_FadeColor2.color !$= "0 0 0 1")
        %node.color = CSC_FadeColor2.color;
    else
        %node.color = "";
}

//This function gets the player or camera's postion and sets the node to that position
function CSC_Editor::setNodePositioning()
{
    //Get what we're controling, whether it's a camera or player
    %tempHolder = LocalClientConnection.getControlObject();
    //Get that object's position
    %tempHolder = %tempHolder.getposition();
    //Set that position as the node's position
    CSC_PositionSelector2.text = %tempHolder;
}

//This function renames all paths and nodes in order
function CSC_Editor::renamePaths()
{
    CSC_Editor::editorDeselectObject();
    %nodeNameCount = 0;

    for (%i = 0; %i < $pathMarkerCount - 1; %i++)
    {
        //Get the name of what we're dealing with now
        %path = CSC_PathEditorSelector.getTextById(%i);
        %count = %path.getCount();

        for (%j = 0; %j < %count; %j++)
        {
            %node = %path.getObject(%j);
            %nodeNameCount++;

            //Rename the node, giving it a 0 in front if needed
            if (%nodeNameCount < 10)
                %node.setName("Node0" @ %nodeNameCount);
            else
                %node.setName("Node" @ %nodeNameCount);
        }

        //Rename the path, giving it a 0 in front if needed
        if (%i < 9)
            %path.setName("Path0" @ %i + 1);
        else
            %path.setName("Path" @ %i + 1);
    }

    //Update all lists
    CSC_Editor.initEditor();
    CSC_Editor.updatePathControls();
}

//This function deletes the currently selected path
function CSC_Editor::deleteAiPath(%this)
{
    CSC_Editor::editorDeselectObject();

	%data = CSC_PathEditorSelector2.getText();

    if (isObject(%data))
        %data.delete();

	CSC_Editor.initEditor();
    CSC_Editor.updatePathControls();
}

//This function deletes the currently selected node
function CSC_Editor::deleteAiNode(%this)
{
    CSC_Editor::editorDeselectObject();

    %path = CSC_PathEditorSelector2.getText();
	%data = CSC_NodeSelector2.getText();

    if (isObject(%data))
        %data.delete();

	CSC_Editor.initEditor();
    CSC_Editor.updatePathControls();
    CSC_PathEditorSelector.setText(%path);
    CSC_PathEditorSelector2.setText(%path);
}

//Make a new path and copy the old nodes to it
function clonePathWithOffset()
{
    %path = CSC_PathEditorSelector.getText();

    //Make a new path
    CSC_Editor.savePathEffect();
    %pathNew = CSC_PathEditorSelector.getText();
    %pathNew.isLooping = %path.isLooping;
    %pathNew.isPingPong = %path.isPingPong;
    %pathNew.loopCount = %path.loopCount;
    %pathNew.nextPath = %path.nextPath;
    %pathNew.data = %path.data;
    %pathNew.callFunction = %path.callFunction;

    if (getField(%path.getGroup().getName(), 0) !$= "MissionGroup")
        %path.getGroup().add(%pathNew);

    %count = %path.getCount();

    //Copy all the old node settings to the new path
    for (%j = 0; %j < %count; %j++)
    {
        %node = %path.getObject(%j);

        %name = "Node" @ getRandom(1, 9999);

        if (isObject(%name))
            %name = "Node" @ getRandom(1, 9999);

        %marker = new Marker(%name) {
            canSaveDynamicFields = "1";
            rotation = "1 0 0 0";
            scale = "1 1 1";
            type = %node.type;
            smoothingType = %node.smoothingType;
            position = %node.getPosition();
            seqNum = %node.seqNum;
            msToNext = %node.msToNext;
            timeScale = %node.timeScale;
            fov = %node.fov;
            shake = %node.shake;
            speed = %node.speed;
            fade = %node.fade;
            black = %node.black;
            color = %node.color;
            mode = %node.mode;
            axis = %node.axis;
            degree = %node.degree;
            location = %node.location;
            offset = %node.offset;
            object = %node.object;
            zoom = %node.zoom;
        };

	    %pathNew.add(%marker);
    }

    //Apply an offset
    CSCEMoveWholePath(%pathNew);

    //Update all lists
    CSC_Editor.initEditor();
    CSC_Editor.updatePathControls();

    CSC_PathEditorSelector.setSelected(CSC_PathEditorSelector.findText(%pathNew));
}

//Move each node on the path a certain amount
function CSCEMoveWholePath(%path)
{
    if (%path $= "")
        %path = CSC_PathEditorSelector.getText();

    %offset = CSC_PathOffsetSelector.getText();

    moveWholePath(%path, %offset);
}

//Reverse the direction that the bot will travel the path in
function CSCEReversePathOrder()
{
    %path = CSC_PathEditorSelector.getText();

    reversePathOrder(%path);
}

//Flipping is really just rescaling by -200%
function flipThePath()
{
    %flip = CSC_PathFlipSelector.getText();

    switch$(%flip)
    {
        case "X":
            %scale = "-200 0 0";
        case "Y":
            %scale = "0 -200 0";
        case "Z":
            %scale = "0 0 -200";
        case "All":
            %scale = "-200 -200 -200";
    }

    CSCERescalePath(%scale);
}

//Change the scale of a path by moving each node a % to/from the center
function CSCERescalePath(%scale)
{
    %path = CSC_PathEditorSelector.getText();

    if (%scale $= "")
        %scale = CSC_PathScaleSelector.getText();

    rescalePath(%path, %scale);
}

//Merge all nodes from two different paths
function mergePathNodes(%path1, %path2)
{
    //Have this as an if statement so it could be called from script if needed
    if (%path1 $= "")
        %path1 = CSC_PathEditorSelector.getText();

    if (%path2 $= "")
        %path2 = CSC_PathEditorSelector3.getText();

    if (!isObject(%path1))
        return;

    if (%path1 $= %path2)
        return;

    %count1 = %path1.getCount();
    %count2 = %path2.getCount();

    //Change around each node's seqNum then move it to its new path
    for (%j = 0; %j < %count1; %j++)
    {
        %node = %path1.getObject(0);
        %node.seqNum = %count2 + %j;
        %path2.add(%node);
    }

    %path1.delete();

    //Update all lists
    CSC_Editor.initEditor();
    CSC_Editor.updatePathControls();

    CSC_PathEditorSelector.setSelected(CSC_PathEditorSelector.findText(%path2));
}

//Take nodes off a current path and add them to a new path
function splitPathNodes(%path, %split)
{
    //Have this as an if statement so it could be called from script if needed
    if (%path $= "")
        %path = CSC_PathEditorSelector.getText();

    if (%split $= "")
        %split = CSC_PathSplitSelector.getText();

    %count = %path.getCount();

    if (!isObject(%path) || %split < 1 || %split > %count)
        return;

    if ($pathMarkerCount < 10)
       %name = "Path0" @ $pathMarkerCount;
    else
       %name = "Path" @ $pathMarkerCount;

    if (isObject(%name))
        %name = "Path" @ getRandom(1, 999);

    %pathNew = new Path(%name) {
        canSaveDynamicFields = "1";
        isLooping = %path.isLooping;
        isPingPong = %path.isPingPong;
        loopCount = %path.loopCount;
        nextPath = %path.nextPath;
        data = %path.data;
        callFunction = %path.callFunction;
    };

    %path.getGroup().add(%pathNew);

    //Change around each node's seqNum then move it to its new path
    for (%j = %split; %j < %count; %j++)
    {
        %node = %path.getObject(%split);
        %node.seqNum = %j - (%split - 1);
        %pathNew.add(%node);
    }

    //Update all lists
    CSC_Editor.initEditor();
    CSC_Editor.updatePathControls();

    CSC_PathEditorSelector.setSelected(CSC_PathEditorSelector.findText(%name));
}

//This function syncs both path selectors
function CSC_PathEditorSelector::onSelect(%this, %obj)
{
    %selected = CSC_PathEditorSelector.getText();
    CSC_PathEditorSelector2.setText(%selected);

    CSC_Editor.updatePathControls();
    CSC_Editor.populateNodelist();

    CSC_EditorText99.setText(%selected.getCount());
}

//This function syncs both path selectors
function CSC_PathEditorSelector2::onSelect(%this, %obj)
{
    %selected = CSC_PathEditorSelector2.getText();
    CSC_PathEditorSelector.setText(%selected);

    CSC_Editor.updatePathControls();
    CSC_Editor.populateNodelist();

    CSC_EditorText99.setText(%selected.getCount());
}
