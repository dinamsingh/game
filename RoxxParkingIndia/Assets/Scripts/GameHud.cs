using System;
using UnityEngine;
using UnityEngine.UI;

namespace RoxxParking
{
    public sealed class GameHud
    {
        private readonly RoxxGame game;
        private readonly Font font;
        private readonly RectTransform safe;
        private readonly GameObject driving;
        private readonly GameObject overlay;
        private readonly RectTransform card;
        private readonly Text speed;
        private readonly Text level;
        private readonly Text hint;
        private readonly Image progress;
        private readonly Text gear;
        private readonly GameObject wheelRoot;
        private readonly GameObject arrows;
        private readonly RectTransform wheel;
        private readonly RectTransform accelerator, brake, leftArrow, rightArrow;
        private readonly Text mode;
        private readonly Text timer, objective;
        private readonly RectTransform compass;
        private int coursePage;
        private readonly Color ink = new Color(0.055f, 0.09f, 0.11f, 0.97f);
        private readonly Color white = new Color(0.94f, 0.95f, 0.9f);
        // v3 HUD: brake must be instantly distinguishable from the accelerator,
        // so it is red (not the old near-black ink) and at least as large as ACCEL.
        private readonly Color brakeRed = new Color(0.83f, 0.15f, 0.13f, 0.97f);
        private readonly Color wheelDark = new Color(0.10f, 0.11f, 0.12f, 0.97f);
        private Rect lastSafe;

        public GameHud(RoxxGame owner)
        {
            game = owner;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject root = new GameObject("Game UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = 0.5f;
            safe = Rect("Safe area", root.transform); Stretch(safe);
            driving = Rect("Driving HUD", safe).gameObject; Stretch((RectTransform)driving.transform);
            RectTransform info = Panel("Level info", driving.transform, new Vector2(0, 1), new Vector2(330, -90), new Vector2(620, 150), ink);
            level = Label(info, "01 / THE FIRST PARK", 25, new Vector2(0, 44), new Vector2(580, 36), TextAnchor.MiddleLeft);
            hint = Label(info, "Park fully inside the green bay", 18, new Vector2(0, 0), new Vector2(580, 36), TextAnchor.MiddleLeft);
            objective = Label(info, "", 16, new Vector2(0,-44),new Vector2(580,34),TextAnchor.MiddleLeft);
            objective.color=WorldBuilder.Saffron;
            RectTransform navigation=Panel("Navigation",driving.transform,new Vector2(.5f,1),new Vector2(35,-70),new Vector2(300,104),ink);
            compass=Label(navigation,"^",38,new Vector2(-90,6),new Vector2(55,65)).rectTransform;
            timer=Label(navigation,"00:00",22,new Vector2(35,4),new Vector2(180,72));
            Button("Pause", driving.transform, new Vector2(1, 1), new Vector2(-95, -52), new Vector2(150, 60), "PAUSE", () => game.Pause());
            Button("View", driving.transform, new Vector2(1, 1), new Vector2(-260, -52), new Vector2(150, 60), "CAMERA", () => game.ResetCamera());
            mode = Button("Input mode", driving.transform, new Vector2(0, 1), new Vector2(120, -195), new Vector2(200, 48), "USE BUTTONS", () => game.ToggleInputMode()).GetComponentInChildren<Text>();
            RectTransform meter = Panel("Speed", driving.transform, new Vector2(1, 0), new Vector2(-125, 348), new Vector2(210, 100), ink);
            speed = Label(meter, "00", 48, new Vector2(-28, 4), new Vector2(120, 64));
            Label(meter, "km/h", 16, new Vector2(62, -8), new Vector2(68, 38));
            gear = Button("Gear", driving.transform, new Vector2(1, 0), new Vector2(-125, 265), new Vector2(210, 54), "D  /  R", () => game.ToggleGear()).GetComponentInChildren<Text>();
            gear.fontSize = 22;
            accelerator = Hold("Accelerator", new Vector2(-100, 112), new Vector2(175, 220), "ACCEL", WorldBuilder.Saffron, v => game.ThrottleHeld = v);
            brake = Hold("Brake", new Vector2(-285, 112), new Vector2(175, 220), "BRAKE", brakeRed, v => game.BrakeHeld = v);
            wheelRoot = Rect("Steering wheel", driving.transform).gameObject;
            wheel = (RectTransform)wheelRoot.transform; Place(wheel, new Vector2(0, 0), new Vector2(220, 200), new Vector2(380, 380));
            Image ring = wheelRoot.AddComponent<Image>(); ring.sprite = Ring(); ring.color = wheelDark;
            RectTransform hub = Panel("Hub", wheel, Vector2.one * 0.5f, Vector2.zero, new Vector2(100, 100), ink);
            for (int i = 0; i < 3; i++)
            {
                RectTransform spoke = Panel("Spoke", wheel, Vector2.one * 0.5f, Vector2.zero, new Vector2(24, 160), white);
                spoke.pivot = new Vector2(0.5f, 0); spoke.anchoredPosition = Vector2.zero; spoke.localEulerAngles = new Vector3(0, 0, i * 120);
                spoke.GetComponent<Image>().raycastTarget = false;
            }
            hub.SetAsLastSibling(); Text hubLabel = Label(hub, "R", 38, Vector2.zero, new Vector2(90, 90)); hubLabel.color = WorldBuilder.Saffron;
            wheelRoot.AddComponent<SteeringControl>().Changed = v => game.WheelInput = v;
            arrows = Rect("Arrow controls", driving.transform).gameObject; Stretch((RectTransform)arrows.transform);
            leftArrow = Arrow("Left", new Vector2(105, 125), "<", v => game.LeftHeld = v);
            rightArrow = Arrow("Right", new Vector2(285, 125), ">", v => game.RightHeld = v);
            arrows.SetActive(false);
            RectTransform progressTrack = Panel("Parking hold track", driving.transform, new Vector2(0.5f, 0), new Vector2(0, 95), new Vector2(420, 16), ink);
            RectTransform fill = Panel("Parking hold", progressTrack, new Vector2(0, 0.5f), Vector2.zero, new Vector2(420, 16), WorldBuilder.Emerald);
            fill.pivot = new Vector2(0, 0.5f); fill.anchoredPosition = Vector2.zero;
            progress = fill.GetComponent<Image>();
            Label(driving.transform, "ALIGN  /  BRAKE  /  HOLD", 18, new Vector2(0, 124), new Vector2(440, 32), TextAnchor.MiddleCenter, new Vector2(0.5f, 0));
            overlay = Panel("Menu shade", safe, Vector2.one * 0.5f, Vector2.zero, Vector2.zero, new Color(0.02f, 0.06f, 0.06f, 0.48f)).gameObject;
            Stretch((RectTransform)overlay.transform);
            card = Panel("Menu card", overlay.transform, Vector2.one * 0.5f, Vector2.zero, new Vector2(640, 650), ink);
            ApplySafeArea();
            ApplyControlLayout();
        }

        public void ApplyControlLayout()
        {
            bool wheelMode=wheelRoot.activeSelf;
            wheelRoot.SetActive(false);arrows.SetActive(false);
            bool right=game.SteeringOnRight;
            Vector2 steerAnchor=new Vector2(right?1:0,0),pedalAnchor=new Vector2(right?0:1,0);
            Place(wheel,steerAnchor,new Vector2(right?-220:220,200),new Vector2(380,380));
            Place(leftArrow,steerAnchor,new Vector2(right?-285:105,125),new Vector2(175,185));
            Place(rightArrow,steerAnchor,new Vector2(right?-105:285,125),new Vector2(175,185));
            Place(accelerator,pedalAnchor,new Vector2(right?100:-100,112),accelerator.sizeDelta);
            Place(brake,pedalAnchor,new Vector2(right?285:-285,112),brake.sizeDelta);
            Place((RectTransform)speed.transform.parent,pedalAnchor,new Vector2(right?125:-125,348),new Vector2(210,100));
            Place((RectTransform)gear.transform.parent,pedalAnchor,new Vector2(right?125:-125,265),new Vector2(210,54));
            wheelRoot.SetActive(wheelMode);arrows.SetActive(!wheelMode);
        }

        public void Update()
        {
            if (lastSafe != Screen.safeArea) ApplySafeArea();
            if (!driving.activeSelf) return;
            speed.text = (game.Car.Speed * 3.6f).ToString("00");
            progress.rectTransform.sizeDelta = new Vector2(420 * game.ParkingProgress, 16);
            wheel.localRotation = Quaternion.Euler(0, 0, -game.Car.Steering * 125);
            gear.text = game.CurrentCourse.reverseOnly ? "R / REVERSE ONLY" : game.Car.Gear > 0 ? "[ D ]     R" : "D     [ R ]";
            string signal=game.GateHint;
            hint.text=game.ParkingProgress>0 ? "Hold still..." : game.RouteJustCleared ? "ROUTE POINT CLEARED!" : signal!="" ? signal : game.Elapsed<6 ? game.CurrentCourse.briefing : game.ObjectiveText;
            hint.color=game.RouteJustCleared?WorldBuilder.Emerald:white;
            objective.text=game.IsTrial?"EXPERT TRIAL / "+(game.CurrentCourse.reverseOnly?"REVERSE ONLY":"NO CONTACT") : DistrictArt.Names[game.CurrentCourse.district]+"   /   "+game.CurrentCourse.difficulty+" / 5";
            timer.text=Clock(game.Elapsed)+"\nGOLD "+Clock(game.CurrentCourse.goldTime);
            Vector3 direction=game.ObjectivePosition-game.Car.transform.position;
            float targetHeading=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
            compass.localRotation=Quaternion.Euler(0,0,-Mathf.DeltaAngle(Camera.main.transform.eulerAngles.y,targetHeading));
        }

        public void SetInputMode(bool buttons)
        {
            wheelRoot.SetActive(!buttons); arrows.SetActive(buttons);
            mode.text = buttons ? "USE WHEEL" : "USE BUTTONS";
        }

        public void ShowDriving(int index, string name)
        {
            overlay.SetActive(false); driving.SetActive(true);
            level.text = (index + 1).ToString("00") + " / " + name.ToUpperInvariant();
            progress.rectTransform.sizeDelta = new Vector2(0, 16);
        }

        public void ShowMenu()
        {
            ClearCard();
            LeftCard();
            Label(card, "INDIAN SUV / PARKING TRIALS", 18, new Vector2(0, 265), new Vector2(550, 34)).color = WorldBuilder.Saffron;
            Label(card, "ROXX", 93, new Vector2(0, 181), new Vector2(550, 120));
            Label(card, "PARKING INDIA", 34, new Vector2(0, 103), new Vector2(550, 60));
            Label(card, "One SUV. Five locations. Twelve Expert Trials.", 22, new Vector2(0, 48), new Vector2(550, 40));
            Button("Continue", card, Vector2.one * 0.5f, new Vector2(0, -27), new Vector2(500, 68), "DRIVE / LEVEL " + (game.UnlockedLevel + 1).ToString("00"), () => game.StartLevel(game.UnlockedLevel), true);
            Button("Courses", card, Vector2.one * 0.5f, new Vector2(-130, -109), new Vector2(240, 58), "COURSES", ShowCourses);
            Button("Garage", card, Vector2.one * 0.5f, new Vector2(130, -109), new Vector2(240, 58), "GARAGE", ShowGarage);
            Button("Workshop preview", card, Vector2.one * .5f,new Vector2(-130,-182),new Vector2(240,50),"WORKSHOP PRACTICE",()=>game.StartLevel(4,true));
            Button("Expert trials",card,Vector2.one*.5f,new Vector2(130,-182),new Vector2(240,50),"EXPERT TRIALS",ShowTrials,true);
            Button("Controls",card,Vector2.one*.5f,new Vector2(0,-242),new Vector2(500,43),"CONTROLS",()=>ShowControls(false));
            Label(card, "PC: W/S drive | A/D steer | SPACE brake | R retry\nTrial stars: "+game.TrialStars+" / "+game.MaximumTrialStars+"  /  Earn new garage paints", 14, new Vector2(0, -293), new Vector2(560, 40));
        }

        private void ShowTrials(){coursePage=RoxxGame.CampaignCourseCount/3;ShowCourses();}
        private void ShowCourses()
        {
            int pages=(game.CourseCount+2)/3;coursePage=Mathf.Clamp(coursePage,0,pages-1);
            ClearCard(); Label(card, coursePage>=4?"EXPERT TRIALS":"EXPLORE THE DISTRICTS", 30, new Vector2(0, 266), new Vector2(570, 55));
            Label(card,coursePage>=4?"ALL TRIALS OPEN / "+game.TrialStars+" OF "+game.MaximumTrialStars+" STARS":DistrictArt.Names[game.CourseAt(coursePage*3).district],21,new Vector2(0,205),new Vector2(570,45)).color=WorldBuilder.Saffron;
            for (int row=0; row<3; row++)
            {
                int i=coursePage*3+row;
                if(i>=game.CourseCount)break;
                int selected = i; bool unlocked = i <= game.UnlockedLevel || i>=RoxxGame.CampaignCourseCount;
                CourseDefinition course=game.CourseAt(i);
                string record=game.BestTime(i)>0?" / "+MedalName(game.BestMedal(i))+" "+Clock(game.BestTime(i)):"";
                Button button = Button("Course " + i, card, Vector2.one * 0.5f,
                    new Vector2(0,107-row*106), new Vector2(550,92),
                    (i+1).ToString("00")+"  "+course.name.ToUpperInvariant()+"\n"+(unlocked?"DIFFICULTY "+course.difficulty+"/5"+record:"LOCKED / COMPLETE PREVIOUS COURSE"), () => game.StartLevel(selected), unlocked);
                button.GetComponentInChildren<Text>().fontSize=19;
                button.interactable = unlocked;
            }
            Button("Previous district",card,Vector2.one*.5f,new Vector2(-145,-193),new Vector2(260,44),"< PREVIOUS",()=>{coursePage=(coursePage+pages-1)%pages;ShowCourses();});
            Button("Next district",card,Vector2.one*.5f,new Vector2(145,-193),new Vector2(260,44),"NEXT >",()=>{coursePage=(coursePage+1)%pages;ShowCourses();});
            Button("Back", card, Vector2.one * 0.5f, new Vector2(0, -258), new Vector2(520, 56), "BACK", ShowMenu);
        }

        private void ShowGarage()
        {
            ClearCard(); LeftCard(); Label(card, "YOUR SUV", 40, new Vector2(0, 258), new Vector2(570, 60));
            Label(card, "THAR ROXX / 5 DOOR", 24, new Vector2(0, 175), new Vector2(570, 50));
            Label(card, "Your Thar. Pick your paint.", 20, new Vector2(0, 110), new Vector2(570, 40));
            string[] names = { "SAFFRON", "WHITE", "FOREST", "GRAPHITE", "MIDNIGHT BLUE", "DESERT GOLD" };
            for (int i = 0; i < names.Length; i++)
            {
                int color = i;
                Button button = Button("Paint " + i, card, Vector2.one * 0.5f,
                    new Vector2(i % 2 == 0 ? -135 : 135, 27 - i / 2 * 77), new Vector2(250, 63), names[i]+(game.IsPaintUnlocked(i)?"":"\n"+(i==4?6:15)+" TRIAL STARS"), () => game.SetPaint(color));
                button.interactable=game.IsPaintUnlocked(i);button.GetComponentInChildren<Text>().fontSize=18;
                button.image.color = RoxxGame.PaintColors[i];
                button.GetComponentInChildren<Text>().color = i == 1 ? ink : white;
            }
            Label(card, "EXPERT TRIAL STARS / "+game.TrialStars+" OF "+game.MaximumTrialStars+"", 18, new Vector2(0, -201), new Vector2(550, 36));
            Button("Back", card, Vector2.one * 0.5f, new Vector2(0, -258), new Vector2(520, 56), "BACK", ShowMenu);
        }

        public void ShowPause()
        {
            ClearCard(); Label(card, "TAKE A BREATHER", 37, new Vector2(0, 220), new Vector2(570, 70));
            Button("Resume", card, Vector2.one * 0.5f, new Vector2(0, 105), new Vector2(500, 68), "RESUME", () => game.Resume(), true);
            Button("Controls",card,Vector2.one*.5f,new Vector2(0,25),new Vector2(500,60),"CONTROLS",()=>ShowControls(true));
            Button("Retry", card, Vector2.one * 0.5f, new Vector2(0, -55), new Vector2(500, 60), "RESTART COURSE", () => game.Restart());
            Button("Graphics",card,Vector2.one*.5f,new Vector2(0,-135),new Vector2(500,58),game.HighGraphics?"GRAPHICS / HIGH":"GRAPHICS / BALANCED",()=>game.ToggleGraphics());
            Button("Menu", card, Vector2.one * 0.5f, new Vector2(0, -215), new Vector2(500, 60), "MAIN MENU", () => game.Menu());
        }

        private void ShowControls(bool paused)
        {
            ClearCard();Label(card,"CONTROLS",37,new Vector2(0,255),new Vector2(570,65));
            Label(card,"CAMERA SENSITIVITY",21,new Vector2(0,180),new Vector2(550,35));
            Text value=Label(card,Mathf.RoundToInt(game.CameraSensitivity*100)+"%",25,new Vector2(0,130),new Vector2(500,40));
            RectTransform track=Panel("Camera sensitivity",card,Vector2.one*.5f,new Vector2(0,65),new Vector2(480,58),new Color(.15f,.21f,.23f));
            RectTransform area=Rect("Handle area",track);Stretch(area);area.offsetMin=new Vector2(24,0);area.offsetMax=new Vector2(-24,0);
            RectTransform handle=Panel("Sensitivity handle",area,Vector2.one*.5f,Vector2.zero,new Vector2(48,58),WorldBuilder.Saffron);
            handle.sizeDelta=new Vector2(48,0);
            Slider slider=track.gameObject.AddComponent<Slider>();slider.minValue=.25f;slider.maxValue=2;
            slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.SetValueWithoutNotify(game.CameraSensitivity);
            slider.onValueChanged.AddListener(v=>{game.SetCameraSensitivity(v);value.text=Mathf.RoundToInt(game.CameraSensitivity*100)+"%";});
            Label(card,"SLOW / 25%                                  FAST / 200%",15,new Vector2(0,12),new Vector2(510,35));
            Button side=Button("Steering side",card,Vector2.one*.5f,new Vector2(0,-75),new Vector2(500,64),game.SteeringOnRight?"STEERING / RIGHT":"STEERING / LEFT",null);
            side.onClick.AddListener(()=>{game.SetSteeringSide(!game.SteeringOnRight);side.GetComponentInChildren<Text>().text=game.SteeringOnRight?"STEERING / RIGHT":"STEERING / LEFT";});
            Label(card,"Pedals move to the opposite side.\nLarger wheel and steering buttons are enabled.",18,new Vector2(0,-153),new Vector2(540,62));
            Button("Reset controls",card,Vector2.one*.5f,new Vector2(0,-220),new Vector2(240,44),"RESET DEFAULTS",()=>{game.SetSteeringSide(false);game.SetCameraSensitivity(1);ShowControls(paused);});
            Button("Back",card,Vector2.one*.5f,new Vector2(0,-280),new Vector2(500,48),"BACK / SAVE",()=>{PlayerPrefs.Save();if(paused)ShowPause();else ShowMenu();});
        }

        public void ShowResult(bool win, bool last)
        {
            ClearCard();
            string title=!win?"A LITTLE TOO CLOSE":game.IsPreview?"PRACTICE CLEARED":game.IsNewBest?"NEW PERSONAL BEST":game.IsTrial?"TRIAL CLEARED":"PARK COMPLETE";
            Label(card,title,37,new Vector2(0,221),new Vector2(590,72)).color=win?WorldBuilder.Emerald:WorldBuilder.Saffron;
            string best=game.PreviousBest>0?(game.IsNewBest?"IMPROVED BY "+(game.PreviousBest-game.Elapsed).ToString("0.0")+"s":"BEST / "+Clock(game.PreviousBest)):"FIRST CLEAR";
            Label(card,win?MedalName(game.EarnedMedal)+" / "+Clock(game.Elapsed)+"  /  "+best:"CONTACT / ROUTE "+game.RouteIndex+" OF "+game.RouteCount+"\nR TO RETRY IMMEDIATELY",20,new Vector2(0,150),new Vector2(560,60));
            string nextPaint=game.TrialStars<6?"MIDNIGHT BLUE AT 6 STARS":game.TrialStars<15?"DESERT GOLD AT 15 STARS":"ALL TRIAL PAINTS UNLOCKED";
            string detail=game.IsPreview?"PRACTICE / PROGRESS NOT SAVED":game.IsTrial?"+"+game.NewTrialStars+" STARS / TOTAL "+game.TrialStars+" OF "+game.MaximumTrialStars+"\n"+(game.NewPaint!=""?"PAINT UNLOCKED / "+game.NewPaint:nextPaint):last?"CAMPAIGN COMPLETE / EXPERT TRIALS ARE OPEN":"NEXT COURSE UNLOCKED";
            if(win&&!game.IsPreview&&game.EarnedMedal<3)detail+="\nGOLD NEEDS "+Mathf.Max(0,game.Elapsed-game.CurrentCourse.goldTime).ToString("0.0")+"s FASTER";
            Label(card,win?detail:"Brake before turning. Set up the next attempt.",17,new Vector2(0,79),new Vector2(570,78));
            if(win&&!game.IsPreview)
                Button("Next",card,Vector2.one*.5f,new Vector2(0,0),new Vector2(500,60),last?"EXPERT TRIALS":game.IsTrial?"NEXT TRIAL":"NEXT COURSE",()=>{if(last)ShowTrials();else game.NextLevel();},true);
            Button("Retry", card, Vector2.one * 0.5f, new Vector2(0, win && !game.IsPreview ? -76 : -8), new Vector2(500, 60), "TRY AGAIN", () => game.Restart(), !win);
            Button("Menu", card, Vector2.one * 0.5f, new Vector2(0, -151), new Vector2(500, 60), "MAIN MENU", () => game.Menu());
        }

        private static string Clock(float value) => ((int)value/60).ToString("00")+":"+((int)value%60).ToString("00");
        private static string MedalName(int value) => value==3?"GOLD":value==2?"SILVER":"BRONZE";

        private void ClearCard()
        {
            card.anchorMin = card.anchorMax = Vector2.one * 0.5f;
            card.anchoredPosition = Vector2.zero;
            driving.SetActive(false); overlay.SetActive(true);
            for (int i = card.childCount - 1; i >= 0; i--)
            {
                GameObject child = card.GetChild(i).gameObject; child.SetActive(false); UnityEngine.Object.Destroy(child);
            }
        }

        private void LeftCard()
        {
            card.anchorMin = card.anchorMax = new Vector2(0, 0.5f);
            card.anchoredPosition = new Vector2(350, 0);
        }

        private void ApplySafeArea()
        {
            lastSafe = Screen.safeArea;
            if (Screen.width == 0 || Screen.height == 0) return;
            safe.anchorMin = new Vector2(lastSafe.xMin / Screen.width, lastSafe.yMin / Screen.height);
            safe.anchorMax = new Vector2(lastSafe.xMax / Screen.width, lastSafe.yMax / Screen.height);
            safe.offsetMin = safe.offsetMax = Vector2.zero;
        }

        private RectTransform Arrow(string name, Vector2 pos, string caption, Action<bool> callback)
        {
            Button button = Button(name, arrows.transform, Vector2.zero, pos, new Vector2(175, 185), caption, null);
            button.gameObject.AddComponent<HoldControl>().Changed = callback;
            return (RectTransform)button.transform;
        }
        private RectTransform Hold(string name, Vector2 pos, Vector2 size, string caption, Color color, Action<bool> callback)
        {
            Button button = Button(name, driving.transform, new Vector2(1, 0), pos, size, caption, null);
            button.image.color = color;
            ColorBlock pressed = button.colors; pressed.pressedColor = new Color(color.r * 0.65f, color.g * 0.65f, color.b * 0.65f, color.a); button.colors = pressed;
            button.gameObject.AddComponent<HoldControl>().Changed = callback;
            return (RectTransform)button.transform;
        }
        private Button Button(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string caption, Action action, bool primary = false)
        {
            RectTransform rect = Panel(name, parent, anchor, pos, size, primary ? WorldBuilder.Saffron : new Color(0.15f, 0.21f, 0.23f, 0.96f));
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            ColorBlock colors = button.colors; colors.highlightedColor = new Color(1, 0.91f, 0.8f); colors.pressedColor = new Color(0.7f, 0.75f, 0.72f); button.colors = colors;
            if (action != null) button.onClick.AddListener(() => action());
            Label(rect, caption, size.y > 80 ? 24 : size.x < 270 ? 18 : 24, Vector2.zero, size - new Vector2(15, 8));
            return button;
        }
        private Text Label(Transform parent, string caption, int size, Vector2 pos, Vector2 dimensions,
            TextAnchor alignment = TextAnchor.MiddleCenter, Vector2? anchor = null)
        {
            RectTransform rect = Rect(caption, parent); Place(rect, anchor ?? Vector2.one * 0.5f, pos, dimensions);
            Text text = rect.gameObject.AddComponent<Text>(); text.font = font; text.text = caption; text.fontSize = size;
            text.color = white; text.alignment = alignment; text.raycastTarget = false; text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; return text;
        }
        private static RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            RectTransform rect = Rect(name, parent); Place(rect, anchor, pos, size);
            rect.gameObject.AddComponent<Image>().color = color; return rect;
        }
        private static RectTransform Rect(string name, Transform parent)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
        }
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        { rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = position; rect.sizeDelta = size; }
        private static Sprite Ring()
        {
            const int resolution = 128;
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[resolution * resolution];
            for (int y = 0; y < resolution; y++) for (int x = 0; x < resolution; x++)
            {
                float radius = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(64, 64));
                pixels[y * resolution + x] = new Color(1, 1, 1, Mathf.Clamp01(Mathf.Min(radius - 48, 63 - radius)));
            }
            texture.SetPixels(pixels); texture.Apply();
            return Sprite.Create(texture, new UnityEngine.Rect(0, 0, resolution, resolution), Vector2.one * 0.5f);
        }
    }
}
