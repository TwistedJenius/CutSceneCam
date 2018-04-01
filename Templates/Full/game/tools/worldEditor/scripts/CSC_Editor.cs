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


//Change this to 1 to get rid of the delete path/node buttons,
//leave it 0 to have that option available, this is to prevent accidents
$CSC_DisableDeleteMode = 0;

//When editing a preexisting path/node, after changes are made to the current one, should the next
//one selected be lower or higher? 0 for lower, 1 for higher, anything else for the same
$CSC_SelectNextMarker = 2;


//-----------------------------------------------------------------------------
//Loading Functions
//-----------------------------------------------------------------------------

//This function opens and closes the editor
function toggleCSCEditor(%val)
{
    if (!%val)
        return;

	echo("Toggling Cut Scene Cam Editor");

    //If the editor is open then close, else open it
    if ($CSC_Editor::isOpen)
    {
        Canvas.popDialog(CSC_Editor);
        $CSC_Editor::isOpen = false;
        return;
    }

    %sel = CSC_PathEditorSelector.getText();

    %obj = getFirstSelectedObject();

    CSC_Editor.initEditor();
    Canvas.pushDialog(CSC_Editor);
    $CSC_Editor::isOpen = true;

    %obj = CSC_Editor.setSelectedOnLoad(%obj);

    CSC_Editor.deselectEverything();

    if (!CSC_Editor.setSelectedOnLoad2(%obj))
        CSC_PathEditorSelector.setSelected(CSC_NextPathSelector.findText(%sel) - 1);
}

//Find the first object that's selected that we care about
function getFirstSelectedObject()
{
	if (isObject(EWorldEditor))
    {
	    if (EWorldEditor.isAwake())
        {
            %count = EWorldEditor.getSelectionSize();

            //Clear all selections
            for (%i = 0; %i < %count; %i++)
            {
                %obj = EWorldEditor.getSelectedObject(0);
                %name = %obj.getClassName();

                if (%name $= "Path" || %name $= "Marker")
                    return %obj;
            }
        }
    }

    return 0;
}

//On load, select the correct object in the editor
function CSC_Editor::setSelectedOnLoad(%this, %obj)
{
    if (!isObject(%obj))
        return;

    %name = %obj.getClassName();

    if (%name $= "Path")
        CSC_PageSelector.setText("Paths");
    else if (%name $= "Marker") //Path nodes
        CSC_PageSelector.setText("Nodes");

    return %obj;
}

//On load, select the correct object in the editor
function CSC_Editor::setSelectedOnLoad2(%this, %obj)
{
    if (!isObject(%obj))
        return false;

    %name = %obj.getClassName();

    if (%name $= "Path")
    {
        CSC_PathEditorSelector.setText(%obj.getName());
        CSC_PathEditorSelector2.setText(%obj.getName());

        CSC_Editor.updatePathControls();
        CSC_Editor.populateNodelist();

        CSC_EditorText99.setText(%obj.getCount());
    }
    else if (%name $= "Marker") //Path nodes
    {
        CSC_PathEditorSelector.setText(%obj.parentGroup.getName());
        CSC_PathEditorSelector2.setText(%obj.parentGroup.getName());

        CSC_Editor.updatePathControls();
        CSC_Editor.populateNodelist();

        CSC_EditorText99.setText(%obj.parentGroup.getCount());

        CSC_NodeSelector.setText(%obj.getName());
        CSC_NodeSelector2.setText(%obj.getName());

        CSC_Editor.updateNodeControls();
    }

    return true;
}

//This function updates all the lists in the editor
function CSC_Editor::initEditor(%this)
{
   CSC_PathEditorSelector.clear();
   CSC_PathEditorSelector2.clear();
   CSC_PathEditorSelector3.clear();
   CSC_NextPathSelector.clear();
   CSC_SimGroupSelector2.clear();
   CSC_DatablockSelector.clear();
   CSC_DatablockSelector2.clear();
   CSC_ObjectSelector.clear();

   %count = DatablockGroup.getCount();
   %pathBlockCount = 0;

   //Cycle through all datablocks and get the names of the path camera ones
   for (%i = 0; %i < %count; %i++)
   {
      %data = DatablockGroup.getObject(%i);
      %dataClass = %data.getClassName();

      if (%dataClass $= "PathCameraData")
      {
         CSC_DatablockSelector.add(%data.getName(), %pathBlockCount);
         CSC_DatablockSelector2.add(%data.getName(), %pathBlockCount);
         %pathBlockCount++;
      }
   }

   //Reset varaibles from last time
   $pathMarkerCount = 0;
   $simgroupCount = 0;
   $namedObjectCount = 0;

   CSC_Editor.cycleSimGroups();

   //Keep going until we've gone through every simgroup in the missiongroup
   for (%i = 1; %i <= $simgroupCount; %i++)
   {
      %name = $simNameArray[%i];
      CSC_Editor.cycleSimGroups(%name);
   }

   %botGroup = "allBotsGroup";

   //Look for any bots that are active
   if (isObject(%botGroup))
      CSC_Editor.cycleSimGroups(%botGroup);

   echo("Cut Scene Cam Editor counts: " @ %pathBlockCount @ " path camera datablocks " @ $namedObjectCount @ " named objects, and " @
   $pathMarkerCount @ " paths within " @ $simgroupCount @ " simgroups.");

   $pathMarkerCount++;

   //Add the default, non-dynamic options to already created lists
   CSC_SimGroupSelector2.add("MissionGroup", $simgroupCount++);
   CSC_NextPathSelector.add("-NONE", 0);
   CSC_ObjectSelector.add("-NONE", 0);
   CSC_ObjectSelector.add("-PLAYER (LOCAL)", 1);
   CSC_ObjectSelector.add("-UAISK BOT ONE", 2);

   //Create some new non-dynamic lists
   CSC_PageSelector.clear();
   CSC_PageSelector.add("Creation", 0);
   CSC_PageSelector.add("Paths", 1);
   CSC_PageSelector.add("Nodes", 2);
   CSC_PageSelector.add("Focus", 3);
   CSC_PageSelector.add("Fade", 4);
   CSC_PageSelector.add("Management", 5);
   CSC_PageSelector.add("Actions", 6);

   CSC_FocalModeSelector.clear();
   CSC_FocalModeSelector.add("Direction", 0);
   CSC_FocalModeSelector.add("Ahead", 1);
   CSC_FocalModeSelector.add("Object", 2);
   CSC_FocalModeSelector.add("Location", 3);

   CSC_NextSelector.clear();
   CSC_NextSelector.add("Lower", 0);
   CSC_NextSelector.add("Higher", 1);
   CSC_NextSelector.add("Same", 2);

   // 0 for Z, 1 for X, and 2 for Y
   CSC_AxisSelector.clear();
   CSC_AxisSelector.add("Z", 0);
   CSC_AxisSelector.add("X", 1);
   CSC_AxisSelector.add("Y", 2);

   CSC_PathFlipSelector.clear();
   CSC_PathFlipSelector.add("X", 0);
   CSC_PathFlipSelector.add("Y", 1);
   CSC_PathFlipSelector.add("Z", 2);
   CSC_PathFlipSelector.add("All", 3);

   CSC_SmoothingTypeSelector.clear();
   CSC_SmoothingTypeSelector.add("Spline", 0);
   CSC_SmoothingTypeSelector.add("Linear", 1);

   CSC_TypeSelector.clear();
   CSC_TypeSelector.add("Normal", 0);
   CSC_TypeSelector.add("Kink", 1);
   CSC_TypeSelector.add("Position Only", 2);

   //Sort alphabetically
   CSC_PathEditorSelector.sort();
   CSC_PathEditorSelector2.sort();
   CSC_PathEditorSelector3.sort();
   CSC_NextPathSelector.sort();
   CSC_SimGroupSelector2.sort();
   CSC_ObjectSelector.sort();

   //Select default options
   CSC_PathEditorSelector.setText(CSC_PathEditorSelector.getTextById(0));
   CSC_PathEditorSelector2.setText(CSC_PathEditorSelector2.getTextById(0));
   CSC_PathEditorSelector3.setText(CSC_PathEditorSelector3.getTextById(0));
   CSC_NextPathSelector.setText(CSC_NextPathSelector.getTextById(0));
   CSC_SimGroupSelector2.setSelected($simgroupCount);
   CSC_ObjectSelector.setText(CSC_ObjectSelector.getTextById(0));
   CSC_DatablockSelector.setSelected(CSC_DatablockSelector.findText($CSC_Datablock));
   CSC_DatablockSelector2.setSelected(CSC_DatablockSelector2.findText($CSC_Datablock));
   CSC_PathFlipSelector.setSelected(0);
   CSC_FocalModeSelector.setSelected(0);
   CSC_TimeScaleSelector.text = $timeScale;
   CSC_SmoothingTypeSelector.setSelected(0);
   CSC_TypeSelector.setSelected(0);
   CSC_NextSelector.setSelected($CSC_SelectNextMarker);

   if (isObject(CSC_PathEditorSelector.getText()))
      CSC_EditorText99.setText(CSC_PathEditorSelector.getText().getCount());

   CSC_DodgeSelector.text = 0;

   if (CSC_CreatorEditor.isVisible())
      CSC_PageSelector.setText("Creation");
   else if (CSC_PathsEditor.isVisible())
      CSC_PageSelector.setText("Paths");
   else if (CSC_Actions.isVisible())
      CSC_PageSelector.setText("Actions");
   else if (CSC_PathManager.isVisible())
      CSC_PageSelector.setText("Management");
   else if (CSC_NodeEditor.isVisible())
      CSC_PageSelector.setText("Nodes");
   else if (CSC_FocusEditor.isVisible())
      CSC_PageSelector.setText("Focus");
   else if (CSC_FadeEditor.isVisible())
      CSC_PageSelector.setText("Fade");
   else
      CSC_PageSelector.setText("Creation");

    //Disable the delete buttons if desired
    if ($CSC_DisableDeleteMode != 0)
    {
        CSC_DeletePath.setVisible(0);
        CSC_DeleteNode.setVisible(0);
        CSC_NodeSelector2.setVisible(0);
    }
    else
    {
        CSC_DeletePath.setVisible(1);
        CSC_DeleteNode.setVisible(1);
        CSC_NodeSelector2.setVisible(1);
    }

    CSC_Editor.updatePathControls();
    CSC_Editor.populateNodelist();
}

//This function cycles through simgroups that are inside of the MissionGroup or another simgroup
function CSC_Editor::cycleSimGroups(%this, %name)
{
    //If this is our first cycle, start with the missiongroup
    if (%name $= "")
        %name = "MissionGroup";

    //Get the number of things in this simgroup
    %n = %name.getCount();

    for (%i = 0; %i < %n; %i++)
    {
      //Get the name of what we're dealing with now
      %obj = %name.getObject(%i);

      //Get every object with a valid name and position
      if (%obj.getName() !$= "" && %obj.position !$= "")
      {
         CSC_ObjectSelector.add(%obj.getName(), $namedObjectCount);
         $namedObjectCount++;
      }

      //Is it a path
      if (%obj.getClassName() $= "Path")
      {
         //If the path doesn't have a name, give it one to avoid problems later
         if (%obj.getName() $= "")
         {
            if ($pathMarkerCount < 9)
               %obj.setName("Path0" @ ($pathMarkerCount + 1));
            else
               %obj.setName("Path" @ ($pathMarkerCount + 1));
         }

         CSC_PathEditorSelector.add(%obj.getName(), $pathMarkerCount);
         CSC_PathEditorSelector2.add(%obj.getName(), $pathMarkerCount);
         CSC_PathEditorSelector3.add(%obj.getName(), $pathMarkerCount);
         CSC_NextPathSelector.add(%obj.getName(), $pathMarkerCount + 1);
         $pathMarkerCount++;
      }
      //If this object is a simgroup, get its name so we can go through it later
      else if (%obj.getClassName() $= "SimGroup")
      {
         CSC_SimGroupSelector2.add(%obj.getName(), $simgroupCount);
         $simgroupCount++;
         $simNameArray[$simgroupCount] = %obj;
      }
    }
}


//-----------------------------------------------------------------------------
//Utility Functions
//-----------------------------------------------------------------------------

function CSC_Editor::startCam()
{
    toggleCSCEditor(1);
    CSC_Editor.visible = 0;
    startCameraGoing(CSC_PathNameSelector.getText(), "");
}

//Test camera shaking
function CSC_Editor::TestShake(%this)
{
    %data = CSC_DatablockSelector2.getText();
    %name = "DefaultPathCam";

    new PathCamera(%name)
    {
        dataBlock = %data;
    };

    %time = CSC_ShakeSelector2.getText();
    %name.setCameraShake(%time);
    %name.delete();
}

//This function saves the mission file
function CSC_Editor::MissionFileSave()
{
    //Save the mission
    EWorldEditor.isDirty = false;
    MissionGroup.save($Server::MissionFile);
}

//Deselect whatever is selected
function CSC_Editor::deselectEverything()
{
    %text = CSC_PageSelector.getText();

    if (%text $= "" || %text $= "null")
        return;

	//Set all windows to not visible
    CSC_StartPathButtons.setVisible(false);
    CSC_PathSelectionDrop.setVisible(false);
	CSC_PathsEditor.setVisible(false);
	CSC_Actions.setVisible(false);
	CSC_PathManager.setVisible(false);
    CSC_NodeEditor.setVisible(false);
    CSC_CreatorEditor.setVisible(false);
    CSC_FocusEditor.setVisible(false);
    CSC_FadeEditor.setVisible(false);

    switch$(%text)
    {
        case "Paths":
            //Set the proper windows visible
            CSC_PathSelectionDrop.setVisible(true);
            CSC_StartPathButtons.setVisible(true);
	        CSC_PathsEditor.setVisible(true);
	        //Set the window's name
	        CSC_Window.setFieldValue("text", "Cut Scene Cam Editor - Paths Editor");

        case "Actions":
            CSC_PathSelectionDrop.setVisible(true);
	        CSC_Actions.setVisible(true);
            CSC_StartPathButtons.setVisible(true);
	        CSC_Window.setFieldValue("text", "Cut Scene Cam Editor - Actions");

        case "Management":
            CSC_PathSelectionDrop.setVisible(true);
            CSC_StartPathButtons.setVisible(true);
	        CSC_PathManager.setVisible(true);
	        CSC_Window.setFieldValue("text", "Cut Scene Cam Editor - Path Manager");

        case "Nodes":
            CSC_PathSelectionDrop.setVisible(true);
            CSC_StartPathButtons.setVisible(true);
	        CSC_NodeEditor.setVisible(true);
	        CSC_Window.setFieldValue("text", "Cut Scene Cam Editor - Nodes Editor");

        case "Creation":
	        CSC_CreatorEditor.setVisible(true);
            CSC_StartPathButtons.setVisible(true);
	        CSC_Window.setFieldValue("text", "Cut Scene Cam Editor - Creation");

        case "Focus":
            CSC_PathSelectionDrop.setVisible(true);
            CSC_StartPathButtons.setVisible(true);
	        CSC_FocusEditor.setVisible(true);
	        CSC_Window.setFieldValue("text", "Cut Scene Cam Editor - Focus Editor");

        case "Fade":
            CSC_PathSelectionDrop.setVisible(true);
            CSC_StartPathButtons.setVisible(true);
	        CSC_FadeEditor.setVisible(true);
	        CSC_Window.setFieldValue("text", "Cut Scene Cam Editor - Fade Editor");
    }
}

//Unselect everything then select the correct object
function CSC_Editor::editorSelectObject(%data)
{
	//Check if the world editor is open
	if (isObject(EWorldEditor))
    {
	    if (EWorldEditor.isAwake())
        {
            %count = EWorldEditor.getSelectionSize();

            //Clear all selections
            for (%i = 0; %i < %count; %i++)
            {
                EWorldEditor.unselectObject(EWorldEditor.getSelectedObject(0));
            }

	        //Select the object we're working with
	        EWorldEditor.selectObject(%data);
        }
    }
}

//Unselect everything
function CSC_Editor::editordeselectObject()
{
	//Check if the world editor is open
	if (isObject(EWorldEditor))
    {
	    if (EWorldEditor.isAwake())
        {
            %count = EWorldEditor.getSelectionSize();

            //Clear all selections
            for (%i = 0; %i < %count; %i++)
            {
                EWorldEditor.unselectObject(EWorldEditor.getSelectedObject(0));
            }
        }
    }
}


//-----------------------------------------------------------------------------
//Button Specific Functions
//-----------------------------------------------------------------------------

//This function displays the correct window settings
function CSC_PageSelector::onSelect(%this, %sel, %text)
{
    CSC_Editor::deselectEverything();
}

//Show the proper focal options
function CSC_FocalModeSelector::onSelect(%this, %sel, %text)
{
    CSC_AheadEditor.setVisible(false);
    CSC_LocationEditor.setVisible(false);
    CSC_ObjectEditor.setVisible(false);
    CSC_DirectionEditor.setVisible(false);

    switch$(%text)
    {
        case "Direction":
            CSC_DirectionEditor.setVisible(true);

        case "Location":
            CSC_LocationEditor.setVisible(true);

        case "Ahead":
            CSC_AheadEditor.setVisible(true);

        case "Object":
            CSC_ObjectEditor.setVisible(true);

        default:
            CSC_DirectionEditor.setVisible(true);
    }
}

//If we have a nextPath we don't need to loop or pingpong
function CSC_NextPathSelector::onSelect(%this, %sel, %text)
{
    if (isObject(CSC_NextPathSelector.getText()) && CSC_LoopCountSelector.getText() <= 0)
    {
        CSC_LoopingSelector.setValue("0");
        CSC_PingPongSelector.setValue("0");
    }
}

//If we're looping we don't need a nextPath or to pingpong
function CSC_LoopingSelector::onAction(%this, %obj)
{
    if (CSC_LoopingSelector.getValue() > 0)
    {
        if (CSC_LoopCountSelector.getText() <= 0)
            CSC_NextPathSelector.setSelected(0);

        CSC_PingPongSelector.setValue("0");
    }
}

//If we pingpong we don't need a nextPath or to loop
function CSC_PingPongSelector::onAction(%this, %obj)
{
    if (CSC_PingPongSelector.getValue() > 0)
    {
        if (CSC_LoopCountSelector.getText() <= 0)
            CSC_NextPathSelector.setSelected(0);

        CSC_LoopingSelector.setValue("0");
    }
}

function CSC_NextSelector::onSelect(%this, %sel, %text)
{
    $CSC_SelectNextMarker = %sel;
}
