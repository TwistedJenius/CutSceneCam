//-----------------------------------------------------------------------------
// Copyright (c) 2012 GarageGames, LLC
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

#include "platform/platform.h"
#include "math/mMath.h"
#include "math/mathIO.h"
#include "console/simBase.h"
#include "console/console.h"
#include "console/consoleTypes.h"
#include "core/stream/bitStream.h"
#include "core/dnet.h"
#include "scene/pathManager.h"
#include "app/game.h"
#include "T3D/gameBase/gameConnection.h"
#include "T3D/fx/cameraFXMgr.h"
#include "console/engineAPI.h"
#include "math/mTransform.h"

#include "T3D/pathCamera.h"


//----------------------------------------------------------------------------

IMPLEMENT_CO_DATABLOCK_V1(PathCameraData);

ConsoleDocClass( PathCameraData,
   "@brief General interface to control a PathCamera object from the script level.\n"
   "@see PathCamera\n"
	"@tsexample\n"
		"datablock PathCameraData(LoopingCam)\n"
		"	{\n"
		"		mode = \"\";\n"
		"	};\n"
	"@endtsexample\n"
   "@ingroup PathCameras\n"
   "@ingroup Datablocks\n"
);

// ************************** Cut Scene Cam: Start
PathCameraData::PathCameraData()
{
   camShakeFreq = Point3F(10.0f, 10.0f, 10.0f);
   camShakeAmp = Point3F(1.0f, 1.0f, 1.0f);
   camShakeFalloff = 10.0f;
}
// ************************** Cut Scene Cam: End

void PathCameraData::consoleInit()
{
}

void PathCameraData::initPersistFields()
{
   Parent::initPersistFields();
}

void PathCameraData::packData(BitStream* stream)
{
   Parent::packData(stream);
}

void PathCameraData::unpackData(BitStream* stream)
{
   Parent::unpackData(stream);
}


//----------------------------------------------------------------------------

IMPLEMENT_CO_NETOBJECT_V1(PathCamera);

ConsoleDocClass( PathCamera,
   "@brief A camera that moves along a path. The camera can then be made to travel along this path forwards or backwards.\n\n"

   "A camera's path is made up of knots, which define a position, rotation and speed for the camera.  Traversal from one knot to "
   "another may be either linear or using a Catmull-Rom spline.  If the knot is part of a spline, then it may be a normal knot "
   "or defined as a kink.  Kinked knots are a hard transition on the spline rather than a smooth one.  A knot may also be defined "
   "as a position only.  In this case the knot is treated as a normal knot but is ignored when determining how to smoothly rotate "
   "the camera while it is travelling along the path (the algorithm moves on to the next knot in the path for determining rotation).\n\n"

   "The datablock field for a PathCamera is a previously created PathCameraData, which acts as the interface between the script and the engine "
   "for this PathCamera object.\n\n"

   "@see PathCameraData\n"

		"@tsexample\n"
		   "%newPathCamera = new PathCamera()\n"
		   "{\n"
		   "  dataBlock = LoopingCam;\n"
		   "  position = \"0 0 300 1 0 0 0\";\n"
		   "};\n"
		"@endtsexample\n"

   "@ingroup PathCameras\n"
);

IMPLEMENT_CALLBACK( PathCamera, onNode, void, (S32 node), (node),
					"A script callback that indicates the path camera has arrived at a specific node in its path.  Server side only.\n"
					"@param Node Unique ID assigned to this node.\n");

PathCamera::PathCamera()
{
   mNetFlags.clear(Ghostable);
   mTypeMask |= CameraObjectType;
   delta.time = 0;
   delta.timeVec = 0;

   mDataBlock = 0;
   mState = Forward;
   mNodeBase = 0;
   mNodeCount = 0;
   mPosition = 0;
   mTarget = 0;
   mTargetSet = false;

   // ************************** Cut Scene Cam: Start
   mHeadingPos = Point3F::Zero;
   mHeadingRot = 0.0f;
   mHeadingObj = 0;
   mHeadingOffset = Point3F::Zero;
   mHeadingMode = Direction;
   mHeadingAhead = Point3F::Zero;
   mHeadingDiff = Point3F::Zero;
   mHeadingAxis = 0;
   // ************************** Cut Scene Cam: End

   MatrixF mat(1);
   mat.setPosition(Point3F(0,0,700));
   Parent::setTransform(mat);
}

PathCamera::~PathCamera()
{
}


//----------------------------------------------------------------------------

bool PathCamera::onAdd()
{
   if(!Parent::onAdd())
      return false;

   // Initialize from the current transform.
   if (!mNodeCount) {
      QuatF rot(getTransform());
      Point3F pos = getPosition();
      mSpline.removeAll();
      mSpline.push_back(new CameraSpline::Knot(pos,rot,1,
         CameraSpline::Knot::NORMAL, CameraSpline::Knot::SPLINE));
      mNodeCount = 1;
   }

   //
   mObjBox.maxExtents = mObjScale;
   mObjBox.minExtents = mObjScale;
   mObjBox.minExtents.neg();
   resetWorldBox();

   if (mShapeInstance)
   {
      mNetFlags.set(Ghostable);
      setScopeAlways();
   }

   addToScene();

   return true;
}

void PathCamera::onRemove()
{
   removeFromScene();

   Parent::onRemove();
}

bool PathCamera::onNewDataBlock( GameBaseData *dptr, bool reload )
{
   mDataBlock = dynamic_cast< PathCameraData* >( dptr );
   if ( !mDataBlock || !Parent::onNewDataBlock( dptr, reload ) )
      return false;

   scriptOnNewDataBlock();
   return true;
}

//----------------------------------------------------------------------------

void PathCamera::onEditorEnable()
{
   mNetFlags.set(Ghostable);
}

void PathCamera::onEditorDisable()
{
   mNetFlags.clear(Ghostable);
}


//----------------------------------------------------------------------------

void PathCamera::initPersistFields()
{
   Parent::initPersistFields();
}

void PathCamera::consoleInit()
{
}


//----------------------------------------------------------------------------

void PathCamera::processTick(const Move* move)
{
   // client and server
   Parent::processTick(move);

   // Move to new time
   advancePosition(TickMs);

   // Set new position
   MatrixF mat;
   interpolateMat(mPosition,&mat);
   Parent::setTransform(mat);

   updateContainer();
}

void PathCamera::interpolateTick(F32 dt)
{
   Parent::interpolateTick(dt);
   MatrixF mat;
   interpolateMat(delta.time + (delta.timeVec * dt),&mat);
   // ************************** Cut Scene Cam: Below
   setRenderTransform(mat);
}

// ************************** Cut Scene Cam: Start
void PathCamera::setRenderTransform(const MatrixF& mat)
{
   // This method should never be called on the client.

   // This currently converts all rotation in the mat into
   // rotations around the z and x axis.
   Point3F pos,vec;
   mat.getColumn(1,&vec);
   mat.getColumn(3,&pos);
   Point3F rot(-mAtan2(vec.z, mSqrt(vec.x*vec.x + vec.y*vec.y)),0,-mAtan2(-vec.x,vec.y));
   _setRenderPosition(pos,rot);
}

void PathCamera::_setRenderPosition(const Point3F& pos,const Point3F& rot)
{
   MatrixF xRot, zRot;
   xRot.set(EulerF(rot.x, 0, 0));
   zRot.set(EulerF(0, 0, rot.z));
   MatrixF temp;
   temp.mul(zRot, xRot);
   temp.setColumn(3, pos);
   Parent::setRenderTransform(temp);
}
// ************************** Cut Scene Cam: End

void PathCamera::interpolateMat(F32 pos,MatrixF* mat)
{
   CameraSpline::Knot knot;
   mSpline.value(pos - mNodeBase,&knot);
   // ************************** Cut Scene Cam: Start

   if (mHeadingMode == Object || mHeadingMode == Location)
   {
      //Update the heading position if we're aiming at an object
      if (mHeadingMode == Object && mHeadingObj > 1)
	  {
		 SceneObject* targetObject;
		 Sim::findObject(mHeadingObj, targetObject);

		 if (targetObject)
		 {
			//If this is a shapebase, use its render eye transform to avoid jittering.
			ShapeBase *shape = dynamic_cast<ShapeBase*>((GameBase*)targetObject);

			if (shape != NULL)
			{
				MatrixF ret;
				shape->getRenderEyeTransform(&ret);
				mHeadingPos = ret.getPosition() + mHeadingOffset;
			}
			else
			{
				mHeadingPos = targetObject->getPosition() + mHeadingOffset;
			}
		 }
	  }

	  applyHeading(mHeadingPos, mat, knot);
   }
   else if (mHeadingMode == Ahead)
   {
	   //Predict where we're going based on where we've been,
	   //not the best way to do it, but close enough
	   F32 speed = knot.mSpeed * 2.0f;
	   //Using this->getPosition() causes slight jittering, it should
	   //use knot.mPosition but that isn't updated at this point
	   Point3F realPos = this->getPosition(); //knot.mPosition;
	   Point3F tempPos = Point3F(-1.0f, -1.0f, -1.0f);

	   //If we're still in the same place, keep looking the same way
	   if (mHeadingAhead == realPos)
		  tempPos = mHeadingDiff;
	   else
	   {
		  tempPos = realPos + (mHeadingAhead * tempPos);
		  tempPos *= Point3F(speed, speed, speed);
		  tempPos += realPos;
		  mHeadingAhead = realPos;
		  mHeadingDiff = tempPos;
	   }

	   applyHeading(tempPos, mat, knot);
	   setMaskBits(AheadMask);
   }
   else //if (mHeadingMode == Direction)
   {
	   //Get a position that's far away but in a certain direction
	   //from our current position
	   F32 speed = knot.mSpeed * 8.0f;
	   Point3F realPos = knot.mPosition;
	   Point3F tempPos = Point3F(-1.0f, -1.0f, -1.0f);

	   //If we're still in the same place, keep looking the same way
	   if (mHeadingAhead == realPos)
		  tempPos = mHeadingDiff;
	   else
	   {
		  F32 rot = mHeadingRot;

		  if (rot > 240)
		     rot -= 360;
		  else if (rot < -120)
			 rot += 360;

		  rot = mDegToRad(rot);

		  if (mHeadingAxis == 1)
		  {
		     F32 x2 = mCos(rot) * speed;
			 F32 z2 = mSin(rot) * speed;

			 if (mHeadingRot < 0)
			 {
		        x2 *= -1.0f;
			    z2 *= -1.0f;
			 }

		     x2 += realPos.x;
			 z2 += realPos.z;

		     tempPos = Point3F(x2, realPos.y, z2);
		  }
		  else if (mHeadingAxis == 2)
		  {
		     F32 y2 = mCos(rot) * speed;
			 F32 z2 = mSin(rot) * speed;

			 if (mHeadingRot < 0)
			 {
		        y2 *= -1.0f;
			    z2 *= -1.0f;
			 }

		     y2 += realPos.y;
			 z2 += realPos.z;

		     tempPos = Point3F(realPos.x, y2, z2);
		  }
		  else
		  {
		     F32 x2 = mSin(rot) * speed;
		     F32 y2 = mCos(rot) * speed;

		     x2 += realPos.x;
		     y2 += realPos.y;

		     tempPos = Point3F(x2, y2, realPos.z);
 		  }

		  mHeadingAhead = realPos;
		  mHeadingDiff = tempPos;
	   }

	   applyHeading(tempPos, mat, knot);
	   setMaskBits(AheadMask);
   }
   // ************************** Cut Scene Cam: End
}

void PathCamera::advancePosition(S32 ms)
{
   delta.timeVec = mPosition;
   // ************************** Cut Scene Cam: Below
   U32 didStop = 0;

   // Advance according to current speed
   if (mState == Forward) {
      mPosition = mSpline.advanceTime(mPosition - mNodeBase,ms);
      if (mPosition > F32(mNodeCount - 1))
         mPosition = F32(mNodeCount - 1);
      mPosition += (F32)mNodeBase;
      if (mTargetSet && mPosition >= mTarget) {
         mTargetSet = false;
         mPosition = mTarget;
         mState = Stop;
      }
   }
   else
      if (mState == Backward) {
         mPosition = mSpline.advanceTime(mPosition - mNodeBase,-ms);
		 // ************************** Cut Scene Cam: Start
         if (mPosition < 0)
		 {
            mPosition = 0;
			didStop = 1;
		 }
		 // ************************** Cut Scene Cam: End
         mPosition += mNodeBase;
         if (mTargetSet && mPosition <= mTarget) {
            mTargetSet = false;
            mPosition = mTarget;
            mState = Stop;
         }
      }

   // ************************** Cut Scene Cam: Start
   // Script callbacks
   if (S32(mPosition) != S32(delta.timeVec) || didStop)
      onNode(S32(mPosition - didStop));
   // ************************** Cut Scene Cam: End

   // Set frame interpolation
   delta.time = mPosition;
   delta.timeVec -= mPosition;
}

// ************************** Cut Scene Cam: Start
void PathCamera::applyHeading(Point3F target, MatrixF* mat, CameraSpline::Knot knot)
{
	Point3F Position;
	Position = knot.mPosition;

	Position = target - Position;
	F32 len = Position.len();
	F32 rotX = mAtan2(Position.z, len);
	F32 rotZ = mAtan2(Position.x, Position.y);

	Point3F tempQuatZ = Point3F(0.0f, 0.0f, rotZ);
	QuatF rotQ(tempQuatZ);
	AngAxisF aa;
	aa.set(rotQ);
	TransformF matrix(Point3F::Zero, aa);

	Point3F tempQuatX = Point3F(rotX * -1.0f, 0.0f, 0.0f);
	QuatF rotR(tempQuatX);
	AngAxisF bb;
	bb.set(rotR);
	TransformF matrix2(Point3F::Zero, bb);

	MatrixF m1 = matrix.getMatrix();
	MatrixF m2 = matrix2.getMatrix();
	m1.mul(m2);

    knot.mRotation.setMatrix(mat);
	mat->setColumn(0, m1.getColumn4F(0));
	mat->setColumn(1, m1.getColumn4F(1));
    mat->setPosition(knot.mPosition);
}
// ************************** Cut Scene Cam: End


//----------------------------------------------------------------------------

void PathCamera::getCameraTransform(F32* pos, MatrixF* mat)
{
   // Overide the ShapeBase method to skip all the first/third person support.
   getRenderEyeTransform(mat);

   // Apply Camera FX.
   mat->mul( gCamFXMgr.getTrans() );
}


//----------------------------------------------------------------------------

void PathCamera::setPosition(F32 pos)
{
   mPosition = mClampF(pos, (F32)mNodeBase, (F32)(mNodeBase + mNodeCount - 1));
   MatrixF mat;
   interpolateMat(mPosition,&mat);
   Parent::setTransform(mat);
   setMaskBits(PositionMask);
}

void PathCamera::setTarget(F32 pos)
{
   mTarget = pos;
   mTargetSet = true;
   if (mTarget > mPosition)
      mState = Forward;
   else
      if (mTarget < mPosition)
         mState = Backward;
      else {
         mTargetSet = false;
         mState = Stop;
      }
   setMaskBits(TargetMask | StateMask);
}

void PathCamera::setState(State s)
{
   mState = s;
   setMaskBits(StateMask);
}


//-----------------------------------------------------------------------------

// ************************** Cut Scene Cam: Start
void PathCamera::setHeadingPos(Point3F pos, bool mode)
{
   mHeadingPos = pos;

   if (mode)
      mHeadingMode = Location;

   setMaskBits(HeadingMask);
}

void PathCamera::setHeadingRot(F32 rot, U32 axis, bool mode)
{
   mHeadingRot = rot;
   mHeadingAxis = axis;

   if (mode)
      mHeadingMode = Direction;

   setMaskBits(HeadingMask);
}

void PathCamera::setHeadingObj(S32 targetObject, Point3F offset, bool mode)
{
   mHeadingObj = targetObject;
   mHeadingOffset = offset;

   if (mode)
      mHeadingMode = Object;

   setMaskBits(HeadingMask);
}

void PathCamera::setHeadingMode(HeadingMode r)
{
   mHeadingMode = r;
   setMaskBits(HeadingMask);
}

void PathCamera::resetHeading()
{
   mHeadingPos = Point3F::Zero;
   mHeadingRot = 0.0f;
   mHeadingObj = 0;
   mHeadingOffset = Point3F::Zero;
   mHeadingAhead = Point3F::Zero;
   mHeadingDiff = Point3F::Zero;
   mHeadingMode = Direction;
   mHeadingAxis = 0;
   setMaskBits(HeadingMask | AheadMask);
}

void PathCamera::setCameraShake(F32 duration)
{
    CameraShake *camShake = new CameraShake;
    camShake->setDuration(duration);
    camShake->setFrequency(mDataBlock->camShakeFreq);
    camShake->setAmplitude(mDataBlock->camShakeAmp);
    camShake->setFalloff(mDataBlock->camShakeFalloff);
    camShake->init();
    gCamFXMgr.addFX(camShake);
}
// ************************** Cut Scene Cam: End


//-----------------------------------------------------------------------------

void PathCamera::reset(F32 speed)
{
   CameraSpline::Knot *knot = new CameraSpline::Knot;
   mSpline.value(mPosition - mNodeBase,knot);
   if (speed)
      knot->mSpeed = speed;
   mSpline.removeAll();
   mSpline.push_back(knot);

   mNodeBase = 0;
   mNodeCount = 1;
   mPosition = 0;
   mTargetSet = false;
   mState = Forward;
   // ************************** Cut Scene Cam: Start
   mHeadingPos = Point3F::Zero;
   mHeadingRot = 0.0f;
   mHeadingObj = 0;
   mHeadingOffset = Point3F::Zero;
   mHeadingAhead = Point3F::Zero;
   mHeadingDiff = Point3F::Zero;
   mHeadingMode = Direction;
   mHeadingAxis = 0;
   setMaskBits(StateMask | PositionMask | WindowMask | TargetMask | HeadingMask | AheadMask);
   // ************************** Cut Scene Cam: End
}

void PathCamera::pushBack(CameraSpline::Knot *knot)
{
   // Make room at the end
   if (mNodeCount == NodeWindow) {
      delete mSpline.remove(mSpline.getKnot(0));
      mNodeBase++;
   }
   else
      mNodeCount++;

   // Fill in the new node
   mSpline.push_back(knot);
   setMaskBits(WindowMask);

   // Make sure the position doesn't fall off
   if (mPosition < mNodeBase) {
      mPosition = (F32)mNodeBase;
      setMaskBits(PositionMask);
   }
}

void PathCamera::pushFront(CameraSpline::Knot *knot)
{
   // Make room at the front
   if (mNodeCount == NodeWindow)
      delete mSpline.remove(mSpline.getKnot(mNodeCount));
   else
      mNodeCount++;
   mNodeBase--;

   // Fill in the new node
   mSpline.push_front(knot);
   setMaskBits(WindowMask);

   // Make sure the position doesn't fall off
   if (mPosition > F32(mNodeBase + (NodeWindow - 1)))
   {
      mPosition = F32(mNodeBase + (NodeWindow - 1));
      setMaskBits(PositionMask);
   }
}

void PathCamera::popFront()
{
   if (mNodeCount < 2)
      return;

   // Remove the first node. Node base and position are unaffected.
   mNodeCount--;
   delete mSpline.remove(mSpline.getKnot(0));

   if( mPosition > 0 )
      mPosition --;
}


//----------------------------------------------------------------------------

void PathCamera::onNode(S32 node)
{
   if (!isGhost())
		onNode_callback(node);
   
}

U32 PathCamera::packUpdate(NetConnection *con, U32 mask, BitStream *stream)
{
   Parent::packUpdate(con,mask,stream);

   if (stream->writeFlag(mask & StateMask))
      stream->writeInt(mState,StateBits);

   // ************************** Cut Scene Cam: Start
   if (stream->writeFlag(mask & HeadingMask))
   {
	  stream->writeInt(mHeadingMode, HeadingModeBits);
	  mathWrite(*stream, mHeadingPos);
	  stream->write(mHeadingRot);
	  stream->write(mHeadingOffset.x);
	  stream->write(mHeadingOffset.y);
	  stream->write(mHeadingOffset.z);
	  stream->writeInt(mHeadingObj, 32);
	  stream->writeInt(mHeadingAxis, 4);
   }

   if (stream->writeFlag(mask & AheadMask))
   {
	  mathWrite(*stream, mHeadingAhead);
	  mathWrite(*stream, mHeadingDiff);
   }
   // ************************** Cut Scene Cam: End

   if (stream->writeFlag(mask & PositionMask))
      stream->write(mPosition);

   if (stream->writeFlag(mask & TargetMask))
      if (stream->writeFlag(mTargetSet))
         stream->write(mTarget);

   if (stream->writeFlag(mask & WindowMask)) {
      stream->write(mNodeBase);
      stream->write(mNodeCount);
      for (S32 i = 0; i < mNodeCount; i++) {
         CameraSpline::Knot *knot = mSpline.getKnot(i);
         mathWrite(*stream, knot->mPosition);
         mathWrite(*stream, knot->mRotation);
         stream->write(knot->mSpeed);
         stream->writeInt(knot->mType, CameraSpline::Knot::NUM_TYPE_BITS);
         stream->writeInt(knot->mPath, CameraSpline::Knot::NUM_PATH_BITS);
      }
   }

   // The rest of the data is part of the control object packet update.
   // If we're controlled by this client, we don't need to send it.
   if(stream->writeFlag(getControllingClient() == con && !(mask & InitialUpdateMask)))
      return 0;

   return 0;
}

void PathCamera::unpackUpdate(NetConnection *con, BitStream *stream)
{
   Parent::unpackUpdate(con,stream);

   // StateMask
   if (stream->readFlag())
      mState = stream->readInt(StateBits);

   // ************************** Cut Scene Cam: Start
   if (stream->readFlag())
   {
	  mHeadingMode = stream->readInt(HeadingModeBits);
	  mathRead(*stream, &mHeadingPos);
	  stream->read(&mHeadingRot);
	  stream->read(&mHeadingOffset.x);
	  stream->read(&mHeadingOffset.y);
	  stream->read(&mHeadingOffset.z);
	  mHeadingObj = stream->readInt(32);
	  mHeadingAxis = stream->readInt(4);
   }

   if (stream->readFlag())
   {
	  mathRead(*stream, &mHeadingAhead);
	  mathRead(*stream, &mHeadingDiff);
   }
   // ************************** Cut Scene Cam: End

   // PositionMask
   if (stream->readFlag()) 
   {
      stream->read(&mPosition);
      delta.time = mPosition;
      delta.timeVec = 0;
   }

   // TargetMask
   if (stream->readFlag())
   {
      mTargetSet = stream->readFlag();
      if (mTargetSet)
         stream->read(&mTarget);
   }

   // WindowMask
   if (stream->readFlag()) 
   {
      mSpline.removeAll();
      stream->read(&mNodeBase);
      stream->read(&mNodeCount);
      for (S32 i = 0; i < mNodeCount; i++)
      {
         CameraSpline::Knot *knot = new CameraSpline::Knot();
         mathRead(*stream, &knot->mPosition);
         mathRead(*stream, &knot->mRotation);
         stream->read(&knot->mSpeed);
         knot->mType = (CameraSpline::Knot::Type)stream->readInt(CameraSpline::Knot::NUM_TYPE_BITS);
         knot->mPath = (CameraSpline::Knot::Path)stream->readInt(CameraSpline::Knot::NUM_PATH_BITS);
         mSpline.push_back(knot);
      }
   }

   // Controlled by the client?
   if (stream->readFlag())
      return;

}


//-----------------------------------------------------------------------------
// Console access methods
//-----------------------------------------------------------------------------

DefineEngineMethod(PathCamera, setPosition, void, (F32 position),(0.0f), "Set the current position of the camera along the path.\n"
													"@param position Position along the path, from 0.0 (path start) - 1.0 (path end), to place the camera.\n"
													"@tsexample\n"
                                          "// Set the camera on a position along its path from 0.0 - 1.0.\n"
														"%position = \"0.35\";\n\n"
														"// Force the pathCamera to its new position along the path.\n"
														"%pathCamera.setPosition(%position);\n"
													"@endtsexample\n")
{
   object->setPosition(position);
}

DefineEngineMethod(PathCamera, setTarget, void, (F32 position),(1.0f), "@brief Set the movement target for this camera along its path.\n\n"
                                       "The camera will attempt to move along the path to the given target in the direction provided "
                                       "by setState() (the default is forwards).  Once the camera moves past this target it will come "
                                       "to a stop, and the target state will be cleared.\n"
													"@param position Target position, between 0.0 (path start) and 1.0 (path end), for the camera to move to along its path.\n"
													"@tsexample\n"
                                          "// Set the position target, between 0.0 (path start) and 1.0 (path end), for this camera to move to.\n"
														"%position = \"0.50\";\n\n"
														"// Inform the pathCamera of the new target position it will move to.\n"
														"%pathCamera.setTarget(%position);\n"
													"@endtsexample\n")
{
   object->setTarget(position);
}

DefineEngineMethod(PathCamera, setState, void, (const char* newState),("forward"), "Set the movement state for this path camera.\n"
													"@param newState New movement state type for this camera. Forward, Backward or Stop.\n"
													"@tsexample\n"
														"// Set the state type (forward, backward, stop).\n"
                                          "// In this example, the camera will travel from the first node\n"
                                          "// to the last node (or target if given with setTarget())\n"
														"%state = \"forward\";\n\n"
														"// Inform the pathCamera to change its movement state to the defined value.\n"
														"%pathCamera.setState(%state);\n"
													"@endtsexample\n")
{
   if (!dStricmp(newState,"forward"))
      object->setState(PathCamera::Forward);
   else
      if (!dStricmp(newState,"backward"))
         object->setState(PathCamera::Backward);
      else
         object->setState(PathCamera::Stop);
}

DefineEngineMethod(PathCamera, reset, void, (F32 speed),(1.0f), "@brief Clear the camera's path and set the camera's current transform as the start of the new path.\n\n"
                                       "What specifically occurs is a new knot is created from the camera's current transform.  Then the current path "
                                       "is cleared and the new knot is pushed onto the path.  Any previous target is cleared and the camera's movement "
                                       "state is set to Forward.  The camera is now ready for a new path to be defined.\n"
													"@param speed Speed for the camera to move along its path after being reset.\n"
													"@tsexample\n"
														"//Determine the new movement speed of this camera. If not set, the speed will default to 1.0.\n"
														"%speed = \"0.50\";\n\n"
														"// Inform the path camera to start a new path at"
                                          "// the camera's current position, and set the new "
                                          "// path's speed value.\n"
														"%pathCamera.reset(%speed);\n"
                                       "@endtsexample\n")
{
	object->reset(speed);
}

static CameraSpline::Knot::Type resolveKnotType(const char *arg)
{
   if (dStricmp(arg, "Position Only") == 0) 
      return CameraSpline::Knot::POSITION_ONLY;
   if (dStricmp(arg, "Kink") == 0) 
      return CameraSpline::Knot::KINK;
   return CameraSpline::Knot::NORMAL;
}

static CameraSpline::Knot::Path resolveKnotPath(const char *arg)
{
   if (!dStricmp(arg, "Linear"))
      return CameraSpline::Knot::LINEAR;
   return CameraSpline::Knot::SPLINE;
}

DefineEngineMethod(PathCamera, pushBack, void, (TransformF transform, F32 speed, const char* type, const char* path),
											   (1.0f, "Normal", "Linear"), 
											      "@brief Adds a new knot to the back of a path camera's path.\n"
													"@param transform Transform for the new knot.  In the form of \"x y z ax ay az aa\" such as returned by SceneObject::getTransform()\n"
													"@param speed Speed setting for this knot.\n"
													"@param type Knot type (Normal, Position Only, Kink).\n"
													"@param path %Path type (Linear, Spline).\n"
													"@tsexample\n"
														"// Transform vector for new knot. (Pos_X Pos_Y Pos_Z Rot_X Rot_Y Rot_Z Angle)\n"
														"%transform = \"15.0 5.0 5.0 1.4 1.0 0.2 1.0\"\n\n"
														"// Speed setting for knot.\n"
														"%speed = \"1.0\"\n\n"
														"// Knot type. (Normal, Position Only, Kink)\n"
														"%type = \"Normal\";\n\n"
														"// Path Type. (Linear, Spline)\n"
														"%path = \"Linear\";\n\n"
														"// Inform the path camera to add a new knot to the back of its path\n"
														"%pathCamera.pushBack(%transform,%speed,%type,%path);\n"
													"@endtsexample\n")
{
   QuatF rot(transform.getOrientation());

   object->pushBack( new CameraSpline::Knot(transform.getPosition(), rot, speed, resolveKnotType(type), resolveKnotPath(path)) );
}

DefineEngineMethod(PathCamera, pushFront, void, (TransformF transform, F32 speed, const char* type, const char* path),
											   (1.0f, "Normal", "Linear"), 
											      "@brief Adds a new knot to the front of a path camera's path.\n"
													"@param transform Transform for the new knot. In the form of \"x y z ax ay az aa\" such as returned by SceneObject::getTransform()\n"
													"@param speed Speed setting for this knot.\n"
													"@param type Knot type (Normal, Position Only, Kink).\n"
													"@param path %Path type (Linear, Spline).\n"
													"@tsexample\n"
														"// Transform vector for new knot. (Pos_X,Pos_Y,Pos_Z,Rot_X,Rot_Y,Rot_Z,Angle)\n"
														"%transform = \"15.0 5.0 5.0 1.4 1.0 0.2 1.0\"\n\n"
														"// Speed setting for knot.\n"
														"%speed = \"1.0\";\n\n"
														"// Knot type. (Normal, Position Only, Kink)\n"
														"%type = \"Normal\";\n\n"
														"// Path Type. (Linear, Spline)\n"
														"%path = \"Linear\";\n\n"
														"// Inform the path camera to add a new knot to the front of its path\n"
														"%pathCamera.pushFront(%transform, %speed, %type, %path);\n"
													"@endtsexample\n")
{
   QuatF rot(transform.getOrientation());

   object->pushFront( new CameraSpline::Knot(transform.getPosition(), rot, speed, resolveKnotType(type), resolveKnotPath(path)) );
}

DefineEngineMethod(PathCamera, popFront, void, (),, "Removes the knot at the front of the camera's path.\n"
													"@tsexample\n"
														"// Remove the first knot in the camera's path.\n"
														"%pathCamera.popFront();\n"
													"@endtsexample\n")
{
   object->popFront();
}

// ************************** Cut Scene Cam: Start
DefineEngineMethod(PathCamera, setHeadingPos, void, (Point3F pos, bool mode), (true), "@brief Set this path camera's focal point to a position in space.\n"
													"@param pos A position in space that will be the focal point for the path camera.\n"
													"@param mode Sets whether or not you would like the path camera to immediately begin using the \"Location\" mode.\n"
													"@tsexample\n"
														"%pos = %obj.getPosition();\n"
														"%pathCamera.setHeadingPos(%pos, true);\n"
													"@endtsexample\n")
{
   object->setHeadingPos(pos, mode);
}

DefineEngineMethod(PathCamera, setHeadingRot, void, (F32 rot, U32 axis, bool mode), (0, true), "@brief Set this path camera's focal point to a direction from the camera's current position.\n"
													"@param rot A value in degrees (-360 to 360) which in would like the camera to point.\n"
													"@param axis Sets the axis the camera should be rotated around. The axes are 0 for Z, 1 for X, and 2 for Y.\n"
													"@param mode Sets whether or not you would like the path camera to immediately begin using the \"Direction\" mode.\n"
													"@tsexample\n"
														"// Set the camera to face in the direction of 90 degrees horizontal.\n"
														"%pathCamera.setHeadingRot(90, 0, true);\n"
													"@endtsexample\n")
{
   if (rot < -360.0f || rot > 360.0f)
      rot = 0.0f;

   if (axis > 3)
	   axis = 0;

   object->setHeadingRot(rot, axis, mode);
}

DefineEngineMethod(PathCamera, setHeadingObj, void, (const char* obj, Point3F offset, bool mode), (Point3F(0.0f, 0.0f, 0.0f), true), 
													"@brief Set this path camera's focal point to an object.\n"
													"@param obj The name of ID of the object you would like the camera to focus on.\n"
													"@param offset An optional offset from that object's position.\n"
													"@param mode Sets whether or not you would like the path camera to immediately begin using the \"Object\" mode.\n"
													"@tsexample\n"
													    "// Set the player object.\n"
														"%obj = LocalClientConnection.player;\n"
														"// Set an offset so we're not looking at their feet.\n"
														"%pathCamera.setHeadingObj(%obj, \"0 0 2\", true);\n"
													"@endtsexample\n")
{
   SceneObject* temp;

   if (Sim::findObject(obj, temp))
      object->setHeadingObj(temp->getId(), offset, mode);
   else
      object->setHeadingObj(0, offset, false);
}

DefineEngineMethod(PathCamera, setHeadingMode, void, (const char* newHeading),("direction"), "@brief Set the type of focal mode we want the camera to use.\n"
													"@param newHeading The new focus mode for this camera. Object, Location, Ahead or Direction.\n"
													"@tsexample\n"
														"// Note that \"Ahead\" is the only mode that cannot be set using another method.\n"
														"%pathCamera.setHeadingMode(\"Ahead\");\n"
													"@endtsexample\n")
{
   if (!dStricmp(newHeading, "object"))
      object->setHeadingMode(PathCamera::Object);
   else if (!dStricmp(newHeading, "location"))
      object->setHeadingMode(PathCamera::Location);
   else if (!dStricmp(newHeading, "ahead"))
      object->setHeadingMode(PathCamera::Ahead);
   else
      object->setHeadingMode(PathCamera::Direction);
}

DefineEngineMethod(PathCamera, getHeadingMode, const char*, (),,
   "@brief Gets the path camera's current focal mode.\n\n"

   "@return Returns the method the path camera is currently using to determine its focal point. "
   "This can be a value of Object, Location, Ahead or Direction."

   "@see setHeadingMode()\n")
{
   S32 mode = object->getHeadingMode();

   if (mode == PathCamera::Object)
      return "Object";
   else if (mode == PathCamera::Location)
      return "Location";
   else if (mode == PathCamera::Ahead)
      return "Ahead";
   else
      return "Direction";
}

DefineEngineMethod(PathCamera, getState, const char*, (),,
   "@brief Gets the path camera's current state type.\n\n"

   "@return Returns the direction the path camera is currently traveling along its path. "
   "This can be a value of Stop, Backward or Forward."

   "@see setState()\n")
{
   S32 state = object->getState();

   if (state == PathCamera::Stop)
      return "Stop";
   else if (state == PathCamera::Backward)
      return "Backward";
   else
      return "Forward";
}

DefineEngineMethod(PathCamera, resetHeading, void, (),, "@brief Clears all focal point settings for the camera, resetting them back to their defaults.\n\n"
													"@tsexample\n"
													"%pathCamera.resetHeading();\n"
													"@endtsexample\n")
{
	object->resetHeading();
}

DefineEngineMethod(PathCamera, setCameraShake, void, (F32 duration),, "@brief Set the path camera to shake based on its datablock settings.\n"
													"@param duration The total duration the shaking should last, given in seconds.\n"
													"@tsexample\n"
														"%pathCamera.setCameraShake(2.5);\n"
													"@endtsexample\n")
{
	if (duration > 0.0f)
      object->setCameraShake(duration);
}
// ************************** Cut Scene Cam: End
