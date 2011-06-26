using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Research.Kinect.Nui;
using Primitives3D;


namespace DucksRevenge
{
    public class DuckWillLeaveUsEventually : Microsoft.Xna.Framework.Game
    {
        Vector3 leftHandPosition = new Vector3(0, 0, 0);
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D kinectRGBVideo;

        private SpriteFont spriteFont;
        private Runtime kinectSensor;
        private SkeletonData skeleton;
        private SpherePrimitive currentPrimitive;
        private CubePrimitive cube;
        private Camera _cam;
        private Matrix view;
        private Matrix projection;
        private Color color = Color.Red;
        private Matrix cubeTranslation;
        private int cubeXPosition;
        private int cubeyYPosition;
        private bool rotationIsEnabled = false;
        private Vector3 finalCubePosition;
        private Matrix cubeMatrix;

        public DuckWillLeaveUsEventually()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            kinectSensor = new Runtime();
          //  kinectSensor.Initialize(RuntimeOptions.UseColor);
            spriteBatch = new SpriteBatch(GraphicsDevice);
            kinectRGBVideo = new Texture2D(GraphicsDevice, 640, 480);
            cube = new CubePrimitive(graphics.GraphicsDevice);


            cubeXPosition = 5;
            cubeyYPosition = 5;
            cubeTranslation = Matrix.CreateTranslation(cubeXPosition, cubeyYPosition, 5);

            currentPrimitive = new SpherePrimitive(graphics.GraphicsDevice, .25f, 5);

            kinectSensor.Initialize(RuntimeOptions.UseColor | RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor);
            kinectSensor.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution640x480, ImageType.ColorYuv);
            kinectSensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(NuiSkeletonFrameReady);
            kinectSensor.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(KinectSensorVideoFrameReady);

            graphics.PreferredBackBufferWidth = 640;
            graphics.PreferredBackBufferHeight = 480;
            graphics.ApplyChanges();
            
            view = Matrix.CreateLookAt(new Vector3(0, 0, 40), new Vector3(0, 0, -100), Vector3.Up);
            projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,
                                                        GraphicsDevice.Viewport.AspectRatio,
                                                        1.0f,
                                                        100);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteFont = Content.Load<SpriteFont>("hudfont");
        }


        private void KinectSensorVideoFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            PlanarImage planarImage = e.ImageFrame.Image;

            var color = new Color[planarImage.Height * planarImage.Width];
            kinectRGBVideo = new Texture2D(graphics.GraphicsDevice, planarImage.Width, planarImage.Height);

            int index = 0;
            for (int y = 0; y < planarImage.Height; y++)
            {
                for (int x = 0; x < planarImage.Width; x++, index += 4)
                {
                    color[y * planarImage.Width + x] = new Color(planarImage.Bits[index + 2], planarImage.Bits[index + 1], planarImage.Bits[index + 0]);
                }
            }

            kinectRGBVideo.SetData(color);
        }

        protected override void UnloadContent()
        {
            kinectSensor.Uninitialize();
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            var keyBoardState = Keyboard.GetState();
            if (keyBoardState.IsKeyDown(Keys.A))
            {
                cubeTranslation = Matrix.Multiply(Matrix.CreateTranslation(-0.05f, 0, 0), cubeTranslation);
            }
            if (keyBoardState.IsKeyDown(Keys.D))
            {
                cubeTranslation = Matrix.Multiply(Matrix.CreateTranslation(0.05f, 0, 0), cubeTranslation);
            }
            if (keyBoardState.IsKeyDown(Keys.W))
            {
                cubeTranslation = Matrix.Multiply(Matrix.CreateTranslation(0, 0.05f, 0), cubeTranslation);
            }
            if (keyBoardState.IsKeyDown(Keys.S))
            {
                cubeTranslation = Matrix.Multiply(Matrix.CreateTranslation(0, -0.05f, 0), cubeTranslation);
            }

            cubeMatrix = rotationIsEnabled
                             ? Matrix.Multiply(Matrix.CreateRotationX((float) (Math.PI/32)),
                                               Matrix.Multiply(Matrix.CreateRotationZ((float) (Math.PI/32)),
                                                               cubeMatrix))
                             : cubeTranslation;

            finalCubePosition = Vector3.Transform(new Vector3(0, 0, 0), cubeTranslation);
            
            base.Update(gameTime);
        }


        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Transparent);

            var handCoords = string.Format("LH-X{0}:: LH-Y: {1}:: LH-Z: {2}", leftHandPosition.X, leftHandPosition.Y, leftHandPosition.Z);
            var cubeCoords = string.Format("C-X{0}:: C-Y: {1}:: C-Z: {2}", finalCubePosition.X, finalCubePosition.Y, finalCubePosition.Z);

            spriteBatch.Begin();
            spriteBatch.Draw(kinectRGBVideo, new Rectangle(0, 0, 640, 480), Color.White);
            spriteBatch.DrawString(spriteFont, handCoords, new Vector2(10, 30), Color.GreenYellow, 0, Vector2.Zero, 1,
                                   SpriteEffects.None, 0);

            spriteBatch.DrawString(spriteFont, cubeCoords, new Vector2(10, 350), Color.GreenYellow, 0, Vector2.Zero, 1,
                                   SpriteEffects.None, 0);
            spriteBatch.End();

            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            DrawPrimitveSkeleton(currentPrimitive, view, projection, Color.LimeGreen);

            kinectSensor.SkeletonEngine.TransformSmooth = true;

            var transformParameters = new TransformSmoothParameters
                                          {
                                              Smoothing = 1.0f,
                                              JitterRadius = 2f
                                          };

               

        kinectSensor.SkeletonEngine.SmoothParameters = transformParameters;

        if ((leftHandPosition.X >= finalCubePosition.X - 2 && leftHandPosition.X <= finalCubePosition.X + 2) && (leftHandPosition.Y >= finalCubePosition.Y - 2 && leftHandPosition.Y <= finalCubePosition.Y + 2 ))
            {
                color = Color.Teal;
                rotationIsEnabled = true;
            }
            else
            {
                color = Color.Red;
                rotationIsEnabled = false;
            }

            cube.Draw(cubeMatrix, view, projection, color);
            base.Draw(gameTime);
        }

        void NuiSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            foreach (SkeletonData s in e.SkeletonFrame.Skeletons)
            {
                if (s.TrackingState == SkeletonTrackingState.Tracked)
                {
                    skeleton = s;
                }
            }
        }

        private void DrawPrimitveSkeleton(GeometricPrimitive primitive, Matrix view, Matrix projection, Color color)
        {
            try
            {
                if (skeleton != null)
                {
                    if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        foreach (Joint joint in skeleton.Joints)
                        {
                            var position = ConvertRealWorldPoint(joint.Position);
                            //position.X = -position.X;
                            Matrix world = new Matrix();
                            world = Matrix.CreateTranslation(position);
                            primitive.Draw(world, view, projection, color);
                        }
                        var leftHand = skeleton.Joints[JointID.HandLeft];
                        leftHandPosition = ConvertRealWorldPoint(leftHand.Position);
                    }
                }
            }
            catch
            {

            }
        }

        private static Vector3 ConvertRealWorldPoint(Vector position)
        {
            var returnVector = new Vector3();
            returnVector.X = position.X * 10;
            returnVector.Y = position.Y * 10;
            returnVector.Z = position.Z;
            return returnVector;
        }
    }
}
