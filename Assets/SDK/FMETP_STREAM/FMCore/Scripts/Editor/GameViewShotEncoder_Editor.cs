using System;
using UnityEngine;
using UnityEditor;

using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(GameViewShotEncoder))]
[CanEditMultipleObjects]
public class GameViewShotEncoder_Editor : Editor
{
    private GameViewShotEncoder GVSEncoder;

    SerializedProperty CaptureModeProp;

    SerializedProperty ResizeProp;

    SerializedProperty RenderCamProp;
    SerializedProperty ResolutionProp;
    SerializedProperty MatchScreenAspectProp;

    SerializedProperty FastModeProp;
    SerializedProperty AsyncModeProp;
    SerializedProperty GZipModeProp;
    SerializedProperty EnableAsyncGPUReadbackProp;

    SerializedProperty QualityProp;
    SerializedProperty ChromaSubsamplingProp;

    SerializedProperty ignoreSimilarTextureProp;
    SerializedProperty similarByteSizeThresholdProp;

    SerializedProperty OnDataByteReadyEventProp;

    SerializedProperty labelProp;
    SerializedProperty dataLengthProp;

    void OnEnable()
    {
        CaptureModeProp = serializedObject.FindProperty("CaptureMode");

        ResizeProp = serializedObject.FindProperty("Resize");

        RenderCamProp = serializedObject.FindProperty("RenderCam");
        ResolutionProp = serializedObject.FindProperty("Resolution");
        MatchScreenAspectProp = serializedObject.FindProperty("MatchScreenAspect");

        FastModeProp = serializedObject.FindProperty("FastMode");
        AsyncModeProp = serializedObject.FindProperty("AsyncMode");
        GZipModeProp = serializedObject.FindProperty("GZipMode");
        EnableAsyncGPUReadbackProp = serializedObject.FindProperty("EnableAsyncGPUReadback");

        QualityProp = serializedObject.FindProperty("Quality");
        ChromaSubsamplingProp = serializedObject.FindProperty("ChromaSubsampling");

        ignoreSimilarTextureProp = serializedObject.FindProperty("ignoreSimilarTexture");
        similarByteSizeThresholdProp = serializedObject.FindProperty("similarByteSizeThreshold");

        OnDataByteReadyEventProp = serializedObject.FindProperty("OnDataByteReadyEvent");

        labelProp = serializedObject.FindProperty("label");
        dataLengthProp = serializedObject.FindProperty("dataLength");
    }

    // Update is called once per frame
    public override void OnInspectorGUI()
    {
        if (GVSEncoder == null) GVSEncoder = (GameViewShotEncoder)target;

        serializedObject.Update();

        GUILayout.Space(10);
        GUILayout.BeginVertical("box");
        {
            {
                //Header
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = 15;

                Texture2D backgroundTexture = new Texture2D(1, 1);
                backgroundTexture.SetPixel(0, 0, new Color(0.09019608f, 0.09019608f, 0.2745098f));
                backgroundTexture.Apply();
                style.normal.background = backgroundTexture;

                GUILayout.BeginHorizontal();
                GUILayout.Label("(( FMETP STREAM CORE V2 ))", style);
                GUILayout.EndHorizontal();
            }

            GUILayout.Label("- Mode");

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(CaptureModeProp, new GUIContent("Capture Mode"));
            GUILayout.EndHorizontal();

            if (GVSEncoder.CaptureMode == GameViewShotCaptureMode.RenderCam)
            {
                GUILayout.BeginVertical("box");
                {
                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = Color.yellow;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("render texture with free aspect", style);
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            else if (GVSEncoder.CaptureMode == GameViewShotCaptureMode.FullScreen)
            {
                GUILayout.BeginVertical("box");
                {
                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = Color.yellow;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("capture full screen with UI Canvas", style);
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }


        }
        GUILayout.EndVertical();


        GUILayout.Space(10);
        GUILayout.BeginVertical("box");
        {
            GUILayout.Label("- Settings");
            GUILayout.BeginVertical("box");
            {
                if (GVSEncoder.CaptureMode == GameViewShotCaptureMode.RenderCam)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(RenderCamProp, new GUIContent("RenderCam"));
                    GUILayout.EndHorizontal();

                    if (GVSEncoder.RenderCam == null)
                    {
                        //GUILayout.BeginVertical("box");
                        {
                            GUIStyle style = new GUIStyle();
                            style.normal.textColor = Color.red;
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(" Render Camera cannot be null", style);
                            GUILayout.EndHorizontal();

                        }
                        //GUILayout.EndVertical();
                    }

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(ResolutionProp, new GUIContent("Resolution"));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(MatchScreenAspectProp, new GUIContent("MatchScreenAspect"));
                    GUILayout.EndHorizontal();
                }

                if (GVSEncoder.CaptureMode == GameViewShotCaptureMode.FullScreen)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(ResizeProp, new GUIContent("Resize"));
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(QualityProp, new GUIContent("Quality"));
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical("box");
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(FastModeProp, new GUIContent("Fast Encode Mode"));
                    GUILayout.EndHorizontal();

                    if (GVSEncoder.FastMode)
                    {
                        GUILayout.BeginVertical("box");
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(AsyncModeProp, new GUIContent("Async Encode (multi-threading)"));
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();

                        GUILayout.BeginVertical("box");
                        {
                            GUILayout.BeginHorizontal();
                            //GUILayout.Label("[ Async GPU Readback Support ]");
                            //GUILayout.Label("Async GPU Readback");

                            GUIStyle style = new GUIStyle();
                            style.normal.textColor = GVSEncoder.SupportsAsyncGPUReadback ? Color.green : Color.gray;
                            GUILayout.Label(" Async GPU Readback (" + (GVSEncoder.SupportsAsyncGPUReadback ? "Supported" : "Unknown or Not Supported") + ")", style);
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(EnableAsyncGPUReadbackProp, new GUIContent("Enabled When Supported"));
                            GUILayout.EndHorizontal();

                        }
                        GUILayout.EndVertical();

                        GUILayout.BeginVertical("box");
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(ChromaSubsamplingProp, new GUIContent("Chroma Subsampling"));
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();
                    }

                    {
                        GUILayout.BeginHorizontal();
                        GUIStyle style = new GUIStyle();
                        style.normal.textColor = Color.yellow;
                        GUILayout.Label(" Experiment for Mac, Windows, Android (Forced Enabled on iOS)", style);
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(GZipModeProp, new GUIContent("GZip Mode", "Reduce network traffic"));
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndVertical();

        GUILayout.Space(10);
        GUILayout.BeginVertical("box");
        {
            GUILayout.BeginVertical("box");
            {
                GUILayout.Label("- Networking");
                GUILayout.BeginVertical("box");
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(ignoreSimilarTextureProp, new GUIContent("ignore Similar Texture"));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(similarByteSizeThresholdProp, new GUIContent("similar Byte Size Threshold"));
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndVertical();

        GUILayout.Space(10);
        GUILayout.BeginVertical("box");
        {
            GUILayout.Label("- Encoded");
            if (GVSEncoder.GetStreamTexture != null)
            {
                GUILayout.Label("Preview " + GVSEncoder.GetStreamTexture.GetType().ToString() + " ( " + GVSEncoder.GetStreamTexture.width + " x " + GVSEncoder.GetStreamTexture.height + " ) ");
            }
            else
            {
                GUILayout.Label("Preview (Empty)");
            }
            GUILayout.BeginVertical("box");
            {
                const float maxLogoWidth = 430.0f;
                EditorGUILayout.Separator();
                float w = EditorGUIUtility.currentViewWidth;
                Rect r = new Rect();
                r.width = Math.Min(w - 40.0f, maxLogoWidth);
                r.height = r.width / 4.886f;
                Rect r2 = GUILayoutUtility.GetRect(r.width, r.height);
                r.x = r2.x;
                r.y = r2.y;
                if (GVSEncoder.GetStreamTexture != null)
                {
                    GUI.DrawTexture(r, GVSEncoder.GetStreamTexture, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUI.DrawTexture(r, new Texture2D((int)r.width, (int)r.height, TextureFormat.RGB24, false), ScaleMode.ScaleToFit);
                }
            }
            GUILayout.EndVertical();

            //GUILayout.BeginHorizontal();
            //EditorGUILayout.PropertyField(CapturedTextureProp, new GUIContent("Captured Texture"));
            //GUILayout.EndHorizontal();

            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(OnDataByteReadyEventProp, new GUIContent("OnDataByteReadyEvent"));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndVertical();


        GUILayout.Space(10);
        GUILayout.BeginVertical("box");
        {
            GUILayout.Label("- Pair Encoder & Decoder ");
            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(labelProp, new GUIContent("label"));
                GUILayout.EndHorizontal();


                //GUILayout.BeginHorizontal();
                //GUILayout.Label("Encoded Size(byte): " + GVEncoder.dataLength);
                //GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(dataLengthProp, new GUIContent("Encoded Size(byte)"));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
}
