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


//Starts the camera on its way
function startCameraGoing(%path, %client)
{
    if (%path $= "")
        %path = $CSC_Name;

    if (!isObject(%path) || !isObject(%path.getObject(0)))
        return;

    if (%client $= "")
        %client = $CSC_Client;

    if (%path.data !$= "")
        %data = %path.data;
    else
        %data = $CSC_Datablock;

    %node = %path.getObject(0);

    //Return for now if we're using a fade
    if (FadeGuiCtrl.done && checkForFades(%node))
    {
        //Wait for half the fade time
        %time = %node.fade + (%node.black / 2);
        schedule(%time, 0, startCameraGoing, %path, %client);
        return;
    }

    //The name is used in other places in the script too
    %name1 = "DefaultPathCam1";

    if (isObject(%name1))
        %name2 = "DefaultPathCam2";
    else
    {
        %name1 = "DefaultPathCam2";
        %name2 = "DefaultPathCam1";
    }

    %client.cam = new PathCamera(%name2)
    {
        dataBlock = %data;
    };

    %name2.followPath(%path);

    MissionCleanup.add(%client.cam);
    %name2.path = %path;
    %name2.client = %client;

    if (isObject(%name1))
    {
        if (%name1.path.nextPath $= %path)
            %control = %name1.control;

        %name1.schedule(120, "delete");
    }

    if (isObject(%control))
        %name2.control = %control;
    else
        %name2.control = %client.getControlObject();

    //Check if we're using AFX
    if (%client.spellbook !$= "")
        %client.setCameraObject(%name2);

    %name2.scopeToClient(%client);
    %client.setControlObject(%name2);

    //Call a function if needed
    if (%path.callFunction !$= "")
        eval(%path.callFunction);
}

//Check if we need to fade
function checkForFades(%node)
{
    //Insert a fade if needed
    if (%node.fade > 0 || %node.black > 0)
    {
        openFadeGui(%node.fade, %node.black, %node.color);
        return true;
    }

    return false;
}

//Add all nodes and initial values for the path to start with
function PathCamera::followPath(%this, %path)
{
    %this.speed = $CSC_Speed;
    %this.setFocusOptions(%path);

    for (%i = 0; %i < %path.getCount(); %i++)
        %this.pushNode(%path.getObject(%i));

    %this.popFront();

    %this.setInitialValues(%path);

    %obj = %path.getObject(0);
    %this.pauseCheck(%obj);
}

function PathCamera::setFocusOptions(%this, %path, %node)
{
    %node = %this.getNode(%path, %node);

    switch$(%node.mode)
    {
        case "Location":
            %this.setHeadingPos(%node.location, true);

        case "Object":
            eval("%obj = " @ %node.object @ ".getId();");

            if (isObject(%obj))
                %this.setHeadingObj(%obj, %node.offset, true);

        case "Ahead":
            %this.setHeadingMode("Ahead");

        case "Direction":
            %this.setHeadingRot(%node.degree, %node.axis, true);
    }
}

function PathCamera::setInitialValues(%this, %path, %node)
{
    %node = %this.getNode(%path, %node);

    //Set fov change speed
    if (%node.zoom > 0)
        setZoomSpeed(%node.zoom);
    else
        setZoomSpeed($pref::Player::zoomSpeed);

    //Fov can't be set until this camera is the control object
    %time = 120;

    //Set fov
    if (%node.fov >= 1 && %node.fov <= 179)
        schedule(%time, %this, "setFov", %node.fov);
    else
        schedule(%time, %this, "setFov", $pref::Player::defaultFov);

    //Shake if needed
    if (%node.shake > 0)
        %this.setCameraShake(%node.shake);

    //Set time scale, but last so it doesn't speed up/slow down the settings above
    if (%node.timeScale > 0)
        $timeScale = %node.timeScale;
    else
        $timeScale = 1;
}

function PathCamera::getNode(%this, %path, %node)
{
    if (isObject(%node))
        return %node;
    else if (isObject(%path))
    {
        //Use the settings from the first node of the path
        %node = %path.getObject(0);

        if (isObject(%node))
            return %node;
    }
    else if (isObject(%this.path))
    {
        %node = %this.path.getObject(0);

        if (isObject(%node))
            return %node;
    }

    return false;
}

//Get the correct settings for the given node
function PathCamera::pushNode(%this, %node)
{
    if (%node.speed <= 0)
        %speed = %this.speed;
    else
        %speed = %node.speed;

    if (%node.type $= "")
        %type = "Normal";
    else
        %type = %node.type;

    if (%node.smoothingType $= "")
        %smoothing = "Spline"; //"Linear";
    else
        %smoothing = %node.smoothingType;

    %this.pushBack(%node.getTransform(), %speed, %type, %smoothing);
}

//Callback from source
function DefaultPathCam1::onNode(%this, %node)
{
    PathCamera::onNodeFilter(%this, %node);
}

function DefaultPathCam2::onNode(%this, %node)
{
    PathCamera::onNodeFilter(%this, %node);
}

function PathCamera::onNodeFilter(%this, %node)
{
    %path = %this.path;
    %count = %path.getCount();
    %client = %this.client;

    //Get the node object rather than just the number
    if (%this.getState() $= "backward")
        %obj = %path.getObject(((%node + 1) % %count));
    else
        %obj = %path.getObject((%node % %count));

    if (%obj.zoom > 0)
        setZoomSpeed(%obj.zoom);

    if (%obj.fov >= 1 && %obj.fov <= 179)
        setFov(%obj.fov);

    if (%obj.shake > 0)
        %this.setCameraShake(%obj.shake);

    //These could be set per node, but aren't
    //checkForFades(%node);
    //%this.setFocusOptions("", %node);
    //%this.setInitialValues("", %node);

    //Is the path a loop
    if (%path.isLooping)
    {
        if (%node == %count && %path.loopCount > 0)
        {
            %this.loopCountDown++;

            if (%this.loopCountDown >= %path.loopCount)
            {
                %this.disengagePath(%client, %obj, %path);
                return;
            }
        }

        if (%node > 0)
            %last = (%node - 1) % %count;
        else
            %last = %count;

        %this.pushNode(%path.getObject(%last));

        //Started a new loop, clear all the nodes from the previous loop
        if (%node == %count + 1)
        {
            for (%i = 0; %i < %count; %i++)
                %this.popFront();
        }
    }
    //Is the path a pingpong
    else if (%path.isPingPong)
    {
        if (%path.loopCount > 0)
        {
            if (%node == %count - 1)
                %this.setState("Backward");
            else if (%node == -1 && %this.loopCountDown < %path.loopCount)
            {
                %this.loopCountDown++;

                if (%this.loopCountDown >= %path.loopCount)
                {
                    %this.disengagePath(%client, %obj, %path);
                    return;
                }

                %this.setState("Forward");
            }
        }
        else
        {
            if (%node == %count - 1)
                %this.setState("Backward");
            else if (%node == -1)
                %this.setState("Forward");
        }
    }
    //Switch back to being player controlled or the using editor
    else if (%node == %count - 1)
    {
        %this.disengagePath(%client, %obj, %path);
        return;
    }

    //Don't pause twice in a row when using pingpong
    if (%this.getState() !$= "backward" || %node < (%count - 1))
        %this.pauseCheck(%obj);
}

function PathCamera::checkEndFade(%this, %client, %path)
{
    if (isObject(%path))
    {
        %count = %path.getCount();

        if (%count > 1)
        {
            %node = %path.getObject(%count -1);

            if (checkForFades(%node))
            {
                %state = %this.getState();
                %this.setState("stop");

                //Pause time in seconds
                %time = %node.fade + (%node.black / 2);
                %this.pauseTimer = %this.schedule(%time, "unPauseTheCamera", %state, %client);
                return true;
            }
        }
    }
    return false;
}

function PathCamera::disengagePath(%this, %client, %obj, %path)
{
    //Check if we need to pause before going back to the player
    if (isObject(%obj))
    {
        if (%this.pauseCheck(%obj, %client))
            return;
    }

    if (%this.nextPathCheck(%obj, %client))
        return;

    if (%this.checkEndFade(%client, %path))
        return;

    %client.setControlObject(%this.control);
    //May not be the real speed, but close enough
    %this.reset(%this.speed);

    //Check if we're using AFX
    if (%client.spellbook !$= "")
        %client.setCameraObject(%client.camera);

    //Reset global values
    $timeScale = 1;
    setZoomSpeed($pref::Player::zoomSpeed);
    setFov($pref::Player::defaultFov);

    %editor = "CSC_Editor";

    //Open the editor again
    if (isObject(%editor) && !%editor.visible)
    {
        toggleCSCEditor(1);
        %editor.visible = 1;
    }

    //Cut scene is over, run any other script as needed
    %this.onEnd(%client, %obj);
}

//Check if we should go to another path now that the current one is finished
function PathCamera::nextPathCheck(%this, %obj, %client)
{
    %path = %this.path;

    //Should we start on another path
    if (%path.nextPath !$= "" && %path.nextPath !$= %path && isObject(%path.nextPath))
    {
        %this.setState("stop");

        //Wait a bit so we don't delete the current camera while its still being used
        schedule(100, 0, startCameraGoing, %path.nextPath, %client);
        return true;
    }

    return false;
}

//Check if the camera should pause
function PathCamera::pauseCheck(%this, %obj, %client)
{
    if (%obj.msToNext > 0)
    {
        %state = %this.getState();
        %this.setState("stop");

        %this.pauseTimer = %this.schedule(%obj.msToNext, "unPauseTheCamera", %state, %client);
        return true;
    }

    return false;
}

function DefaultPathCam1::unPauseTheCamera(%this, %state, %client)
{
    PathCamera::unPauseTheCameraFilter(%this, %state, %client);
}

function DefaultPathCam2::unPauseTheCamera(%this, %state, %client)
{
    PathCamera::unPauseTheCameraFilter(%this, %state, %client);
}

//Set the camera to start moving again
function PathCamera::unPauseTheCameraFilter(%this, %state, %client)
{
    cancel(%this.pauseTimer);
    %this.setState(%state);

    if (isObject(%client))
        %this.disengagePath(%client);
}
