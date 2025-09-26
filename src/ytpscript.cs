// YTScript for Sony Vegas 12-14
// Drop this file into your Vegas Scripts folder or compile as a Vegas script assembly.
// Tested conceptually against ScriptPortal.Vegas API versions; minor name adjustments may be necessary.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ScriptPortal.Vegas; // Sony/VEGAS scripting namespace

public class EntryPoint
{
    // Entry called by Vegas
    public void FromVegas(Vegas vegas)
    {
        // Build a simple UI to let user choose options
        using (var form = new SettingsForm())
        {
            if (form.ShowDialog() != DialogResult.OK)
                return;

            // Apply selected effects to currently selected events or to all events on the active track
            var selectedEvents = GetSelectedEventsOrActiveTrackEvents(vegas, form.ApplyToAll);
            if (selectedEvents == null || selectedEvents.Count == 0)
            {
                MessageBox.Show("No events found to process. Select clip(s) or use Apply to All.", "YTScript");
                return;
            }

            vegas.UndoRedoManager.BeginTransaction("Apply YTScript effects");

            Random rng = new Random(form.RandomSeed);

            foreach (var ev in selectedEvents)
            {
                try
                {
                    if (form.EnableStutter)
                        ApplyStutter(vegas, ev, form.StutterDurationMs, form.StutterRepeats, form.StutterPitchVariance, rng);

                    if (form.EnableStutterPlus)
                        ApplyStutterPlus(vegas, ev, form.StutterPlusBaseMs, form.StutterPlusRepeats, rng);

                    if (form.EnableScramble)
                        ApplyScrambling(vegas, ev, form.ScrambleSliceMs, form.ScrambleDensity, rng);

                    if (form.EnableDanceRave)
                        ApplyDanceRave(vegas, ev, form.RaveIntervalMs, rng);

                    if (form.EnableReverse)
                        ApplyReverse(vegas, ev);

                    if (form.EnableMemeReplace)
                        ApplyMemeReplacement(vegas, ev, form.MemeText, form.MemeDurationMs);

                    if (form.EnableStareZoom)
                        ApplyStareDownZoom(vegas, ev, form.ZoomAmountPercent, form.ZoomDurationMs);

                    if (form.EnableEarRape)
                        ApplyEarRape(vegas, ev, form.EarRapeDbBoost);

                    if (form.EnableBleep)
                        ApplyBleepCensor(vegas, ev, form.BleepDurationMs, form.BleepFrequencyHz);

                    if (form.EnableRandomSound)
                        InsertRandomSoundSample(vegas, ev, form.SampleFolder, rng);

                    if (form.EnablePanning)
                        ApplyAutoPanning(vegas, ev, form.PanLeftRightMs, rng);

                    if (form.EnableTechText)
                        AddRandomTechText(vegas, ev, form.TechTextCount, rng);

                    if (form.EnableSpadinner)
                        InsertRandomSpadinner(vegas, ev, form.SpadinnerFolder, rng);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error processing event: " + ex.Message, "YTScript");
                }
            }

            vegas.UndoRedoManager.EndTransaction("Apply YTScript effects");
            MessageBox.Show("Done applying effects.", "YTScript");
        }
    }

    // UI form with options (simple)
    private class SettingsForm : Form
    {
        public bool ApplyToAll = false;
        public int RandomSeed = Environment.TickCount;

        // Feature toggles & params
        public bool EnableStutter = true;
        public int StutterDurationMs = 50;
        public int StutterRepeats = 8;
        public double StutterPitchVariance = 0.0;

        public bool EnableStutterPlus = false;
        public int StutterPlusBaseMs = 40;
        public int StutterPlusRepeats = 20;

        public bool EnableScramble = false;
        public int ScrambleSliceMs = 100;
        public double ScrambleDensity = 0.8;

        public bool EnableDanceRave = false;
        public int RaveIntervalMs = 120;

        public bool EnableReverse = false;

        public bool EnableMemeReplace = false;
        public string MemeText = "MEME";
        public int MemeDurationMs = 800;

        public bool EnableStareZoom = false;
        public int ZoomAmountPercent = 120;
        public int ZoomDurationMs = 800;

        public bool EnableEarRape = false;
        public double EarRapeDbBoost = 12.0;

        public bool EnableBleep = false;
        public int BleepDurationMs = 200;
        public int BleepFrequencyHz = 1000;

        public bool EnableRandomSound = false;
        public string SampleFolder = "";

        public bool EnablePanning = false;
        public int PanLeftRightMs = 800;

        public bool EnableTechText = false;
        public int TechTextCount = 2;

        public bool EnableSpadinner = false;
        public string SpadinnerFolder = "";

        public SettingsForm()
        {
            Text = "YTScript — options";
            Width = 520;
            Height = 640;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var y = 10;
            int addY(int dh) { y += dh; return y; }

            // Build a few checkboxes and numeric controls (kept short to avoid boxing every control)
            var lbl = new Label() { Text = "Features (enable and tune):", Left = 10, Top = y };
            Controls.Add(lbl);

            addY(24);
            var cbStutter = AddCheck("Stutter loop", EnableStutter, 10, addY(24));
            var numStutterDur = AddNumeric("Stutter ms:", StutterDurationMs, 120, y - 24);
            var numStutterRep = AddNumeric("Repeats:", StutterRepeats, 260, y - 24);

            addY(34);
            var cbStutterPlus = AddCheck("Stutter Loop Plus (varied repeats)", EnableStutterPlus, 10, addY(24));
            var numSPBase = AddNumeric("Base ms:", StutterPlusBaseMs, 120, y - 24);
            var numSPRep = AddNumeric("Max repeats:", StutterPlusRepeats, 260, y - 24);

            addY(34);
            var cbScramble = AddCheck("Scrambling / random chops", EnableScramble, 10, addY(24));
            var numScrSlice = AddNumeric("Slice ms:", ScrambleSliceMs, 120, y - 24);
            var numScrDensity = AddNumeric("Density(%)", (int)(ScrambleDensity * 100), 260, y - 24);

            addY(34);
            var cbRave = AddCheck("Dance / Rave quick-cuts", EnableDanceRave, 10, addY(24));
            var numRaveInterval = AddNumeric("Interval ms:", RaveIntervalMs, 120, y - 24);

            addY(34);
            var cbReverse = AddCheck("Reverse clip", EnableReverse, 10, addY(24));

            addY(34);
            var cbMeme = AddCheck("Meme replacement (text overlay)", EnableMemeReplace, 10, addY(24));
            var txtMeme = new TextBox() { Left = 120, Top = y - 24, Width = 200, Text = MemeText };
            Controls.Add(txtMeme);

            addY(34);
            var cbZoom = AddCheck("Stare Down / Zoom", EnableStareZoom, 10, addY(24));
            var numZoom = AddNumeric("Zoom %:", ZoomAmountPercent, 120, y - 24);

            addY(34);
            var cbEar = AddCheck("Ear-rape (volume burst)", EnableEarRape, 10, addY(24));
            var numEarDb = AddNumeric("DB boost:", (int)EarRapeDbBoost, 120, y - 24);

            addY(34);
            var cbBleep = AddCheck("Add audio bleep/censor", EnableBleep, 10, addY(24));
            var numBleepDur = AddNumeric("Bleep ms:", BleepDurationMs, 140, y - 24);

            addY(34);
            var cbRandSound = AddCheck("Insert random sound sample", EnableRandomSound, 10, addY(24));
            var txtSampleFolder = new TextBox() { Left = 180, Top = y - 24, Width = 260, Text = SampleFolder };
            Controls.Add(txtSampleFolder);

            addY(34);
            var cbPan = AddCheck("Auto panning (L-R)", EnablePanning, 10, addY(24));
            var numPanMs = AddNumeric("Cycle ms:", PanLeftRightMs, 120, y - 24);

            addY(34);
            var cbTech = AddCheck("Random tech text overlays", EnableTechText, 10, addY(24));
            var numTech = AddNumeric("Count:", TechTextCount, 120, y - 24);

            addY(34);
            var cbSpad = AddCheck("Random Spadinner (random sample)", EnableSpadinner, 10, addY(24));
            var txtSpadFolder = new TextBox() { Left = 200, Top = y - 24, Width = 240, Text = SpadinnerFolder };
            Controls.Add(txtSpadFolder);

            addY(42);
            var cbApplyAll = new CheckBox() { Left = 10, Top = y, Text = "Apply to ALL events on active track" };
            Controls.Add(cbApplyAll);

            addY(30);
            var btnOk = new Button() { Text = "Apply", Left = 280, Top = y, Width = 100 };
            var btnCancel = new Button() { Text = "Cancel", Left = 390, Top = y, Width = 100 };
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            // Wire events: on OK, populate public fields from UI
            btnOk.Click += (s, e) =>
            {
                ApplyToAll = cbApplyAll.Checked;
                EnableStutter = cbStutter.Checked;
                StutterDurationMs = (int)numStutterDur.Value;
                StutterRepeats = (int)numStutterRep.Value;

                EnableStutterPlus = cbStutterPlus.Checked;
                StutterPlusBaseMs = (int)numSPBase.Value;
                StutterPlusRepeats = (int)numSPRep.Value;

                EnableScramble = cbScramble.Checked;
                ScrambleSliceMs = (int)numScrSlice.Value;
                ScrambleDensity = (int)numScrDensity.Value / 100.0;

                EnableDanceRave = cbRave.Checked;
                RaveIntervalMs = (int)numRaveInterval.Value;

                EnableReverse = cbReverse.Checked;

                EnableMemeReplace = cbMeme.Checked;
                MemeText = txtMeme.Text;

                EnableStareZoom = cbZoom.Checked;
                ZoomAmountPercent = (int)numZoom.Value;

                EnableEarRape = cbEar.Checked;
                EarRapeDbBoost = (int)numEarDb.Value;

                EnableBleep = cbBleep.Checked;
                BleepDurationMs = (int)numBleepDur.Value;

                EnableRandomSound = cbRandSound.Checked;
                SampleFolder = txtSampleFolder.Text;

                EnablePanning = cbPan.Checked;
                PanLeftRightMs = (int)numPanMs.Value;

                EnableTechText = cbTech.Checked;
                TechTextCount = (int)numTech.Value;

                EnableSpadinner = cbSpad.Checked;
                SpadinnerFolder = txtSpadFolder.Text;

                DialogResult = DialogResult.OK;
                Close();
            };

            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            // Helper methods to create controls
            CheckBox AddCheck(string text, bool @checked, int left, int top)
            {
                var cb = new CheckBox() { Text = text, Checked = @checked, Left = left, Top = top, Width = 300 };
                Controls.Add(cb);
                return cb;
            }

            NumericUpDown AddNumeric(string label, int value, int left, int top)
            {
                var lbl = new Label() { Text = label, Left = left - 80, Top = top + 3, Width = 80 };
                var num = new NumericUpDown() { Left = left, Top = top, Width = 120, Value = value, Maximum = 1000000, Minimum = 0 };
                Controls.Add(lbl);
                Controls.Add(num);
                return num;
            }
        }
    }

    // Utility: get currently selected events or events in active track
    private List<MediaEvent> GetSelectedEventsOrActiveTrackEvents(Vegas vegas, bool applyToAll)
    {
        var events = new List<MediaEvent>();
        if (!applyToAll)
        {
            foreach (Track track in vegas.Project.Tracks)
            {
                foreach (var e in track.Events)
                {
                    var me = e as MediaEvent;
                    if (me != null && me.Selected)
                        events.Add(me);
                }
            }
        }
        else
        {
            // Use active track selected in Vegas UI if available (ActiveTrack)
            Track active = vegas.Project.Tracks.FirstOrDefault(t => t.Selected);
            if (active == null && vegas.Project.Tracks.Count > 0)
                active = vegas.Project.Tracks[0];
            if (active != null)
            {
                foreach (MediaEvent e in active.Events)
                    events.Add(e);
            }
        }

        return events;
    }

    #region Feature implementations

    // 1) Stutter: copy a short slice and repeat it N times
    private void ApplyStutter(Vegas vegas, MediaEvent ev, int sliceMs, int repeats, double pitchVariance, Random rng)
    {
        // Basic algorithm:
        // - Split event at start + sliceDuration
        // - Duplicate the small slice 'repeats' times in place, pushing timeline forward (or overlay)
        // - Optionally shift pitch on each duplicate (Vegas may require adding a take with TimeStretch or pitch plugin)
        Timecode sliceLen = Timecode.FromMilliseconds(sliceMs);

        // Ensure slice shorter than event length
        if (ev.Length <= sliceLen) return;

        Timecode sliceEnd = ev.Start + sliceLen;

        // Split off the slice
        var smallSlice = ev.Split(sliceEnd); // Split returns the right-hand event; we need the left slice — see note below
        // Note: depending on API, Split may return left or right part; adapt if needed.

        // For simplicity: we will duplicate the slice by inserting copies at the same start
        // Because API details vary, use conceptual operations below.

        // Pseudocode because exact API calls may differ:
        // for i in 1..repeats:
        //    copy = DuplicateEvent(smallSlice)
        //    Insert copy at sliceEnd + (i-1)*sliceLen
        //    Optionally adjust pitch/time with Take.AudioStretch or plugin

        // Implementation placeholder (developer should adapt Insert/Duplicate API calls):
        for (int i = 0; i < repeats; i++)
        {
            // The following is conceptual:
            MediaEvent copy = DuplicateEvent(ev, smallSlice.Length);
            double pitchShift = 1.0 + (rng.NextDouble() - 0.5) * pitchVariance;
            // Adjust pitch/time: often you need to use a TimeStretch or pitch effect; left as a hook.
            // Example: ApplyAudioPitchShift(copy, pitchShift);
            InsertEventAt(vegas, copy, smallSlice.Start + Timecode.FromMilliseconds(i * sliceMs));
        }
    }

    // 2) Stutter Plus: more aggressive stutters + randomness
    private void ApplyStutterPlus(Vegas vegas, MediaEvent ev, int baseMs, int maxRepeats, Random rng)
    {
        int repeats = rng.Next(4, Math.Max(5, maxRepeats));
        for (int i = 0; i < repeats; i++)
        {
            int ms = baseMs + rng.Next(-baseMs/2, baseMs/2);
            ApplyStutter(vegas, ev, Math.Max(10, ms), rng.Next(2,8), 0.1 * rng.NextDouble(), rng);
        }
    }

    // 3) Scrambling / random chopping
    private void ApplyScrambling(Vegas vegas, MediaEvent ev, int sliceMs, double density, Random rng)
    {
        int slices = Math.Max(1, (int)(ev.Length.TotalMilliseconds / sliceMs));
        List<MediaEvent> created = new List<MediaEvent>();
        for (int i = 0; i < slices; i++)
        {
            if (rng.NextDouble() > density) continue;
            // Cut slice i
            Timecode start = ev.Start + Timecode.FromMilliseconds(i * sliceMs);
            Timecode end = start + Timecode.FromMilliseconds(sliceMs);
            // Safely split at start and end (pseudo)
            var slice = SafeExtractSlice(vegas, ev, start, end);
            if (slice != null) created.Add(slice);
        }
        // Shuffle and reinsert near original area
        created = created.OrderBy(x => rng.Next()).ToList();
        Timecode pos = ev.Start;
        foreach (var s in created)
        {
            InsertEventAt(vegas, s, pos);
            pos += s.Length;
        }
    }

    // 4) Dance / Rave quick-cuts
    private void ApplyDanceRave(Vegas vegas, MediaEvent ev, int intervalMs, Random rng)
    {
        int cuts = Math.Max(1, (int)(ev.Length.TotalMilliseconds / intervalMs));
        for (int i = 0; i < cuts; i++)
        {
            // small random jump/cut within clip
            var t = ev.Start + Timecode.FromMilliseconds(i * intervalMs) + Timecode.FromMilliseconds(rng.Next(0, intervalMs/3));
            // Create a short slice (interval/2) and optionally insert a jump-cut
            var slice = SafeExtractSlice(vegas, ev, t, t + Timecode.FromMilliseconds(intervalMs/2));
            if (slice != null)
            {
                InsertEventAt(vegas, slice, t);
            }
        }
    }

    // 5) Reverse clip (if audio/video are reversible)
    private void ApplyReverse(Vegas vegas, MediaEvent ev)
    {
        // If event has takes or media with reversable property:
        foreach (Take take in ev.Takes)
        {
            take.Reverse = !take.Reverse; // depending on API, property may be different: take.Reverse or take.IsReverse
        }
    }

    // 6) Meme replacement (text overlay)
    private void ApplyMemeReplacement(Vegas vegas, MediaEvent ev, string text, int durationMs)
    {
        // Create a video text media generator event over the event duration
        Timecode start = ev.Start;
        Timecode dur = Timecode.FromMilliseconds(durationMs);
        // Pseudocode: create a VideoTrack or use overlay track; create a MediaGeneratorEvent with Text
        // Because the API call to create a new text media generator is verbose, leave here an example placeholder:
        // VideoTrack overlay = EnsureOverlayTrack(vegas);
        // var generator = vegas.MediaGenerators["Text"].CreateGenerator(text, ...);
        // overlay.AddEvent(generator, start, dur);
        // For user: adapt using VEGAS MediaGenerator API: vegas.Project.MediaGenerators or vegas.Project.TemplateGenerator ?
    }

    // 7) Stare Down / Zoom (pan/crop automation)
    private void ApplyStareDownZoom(Vegas vegas, MediaEvent ev, int zoomPercent, int zoomDurationMs)
    {
        // Apply Video Motion/Track Motion keyframes (API names vary).
        // Pseudocode:
        // - Create TrackMotion or EventPanCrop automation
        // - Set starting rectangle normal, create keyframe at center with scale = zoomPercent/100
        // See Vegas SDK docs for Pan/Crop property manipulation.
    }

    // 8) Ear-rape: apply gain automation spike
    private void ApplyEarRape(Vegas vegas, MediaEvent ev, double dbBoost)
    {
        // Find audio take, create volume envelope if missing, add a quick spike in the middle of the event
        // Pseudocode because Envelope API varies:
        // var envelope = EnsureVolumeEnvelope(ev);
        // var mid = ev.Start + ev.Length/2;
        // envelope.AddPoint(mid - small, 0);
        // envelope.AddPoint(mid, dbBoost); // dB relative
        // envelope.AddPoint(mid + small, 0);
    }

    // 9) Bleep / censor (create a tone or silence)
    private void ApplyBleepCensor(Vegas vegas, MediaEvent ev, int bleepMs, int frequencyHz)
    {
        // Replace a short portion with a generated tone or silence
        // Pseudocode:
        // - Determine bleep region (random or centered)
        // - Insert a generated audio take (sinewave) of frequencyHz and length bleepMs
        // - Replace audio of that region with the bleep take
    }

    // 10) Insert random sound sample from folder
    private void InsertRandomSoundSample(Vegas vegas, MediaEvent ev, string folder, Random rng)
    {
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;
        var files = Directory.GetFiles(folder).Where(f => IsAudioFile(f)).ToArray();
        if (files.Length == 0) return;
        var pick = files[rng.Next(files.Length)];
        // Insert chosen sample into the timeline at the start of the event (or overlay on an audio track)
        // Pseudocode: find an audio track to put it on and create a new audio event using vegas.AddMediaToTrack(...)
    }

    // 11) Panning automation L-R
    private void ApplyAutoPanning(Vegas vegas, MediaEvent ev, int cycleMs, Random rng)
    {
        // Add pan envelope points across the duration
        // Pseudocode:
        // var panEnv = EnsurePanEnvelope(ev);
        // for t from start to end step cycleMs:
        //    panEnv.AddPoint(t, (tIndex % 2 == 0) ? -1.0 : 1.0); // -1 left, 1 right
    }

    // 12) Random tech text overlays
    private void AddRandomTechText(Vegas vegas, MediaEvent ev, int count, Random rng)
    {
        // Similar to meme replacement but create 'count' short text overlays with random content
        var techWords = new[] { "ERROR", "ACCESS", "0xDEADBEEF", "404", "SYSTEM", "GLITCH", "PROCESS", "STREAM" };
        for (int i = 0; i < count; i++)
        {
            string t = techWords[rng.Next(techWords.Length)];
            int lenMs = rng.Next(150, 1200);
            Timecode pos = ev.Start + Timecode.FromMilliseconds(rng.Next((int)ev.Length.TotalMilliseconds));
            // Create text generator event at pos with lenMs
        }
    }

    // 13) Random Spadinner (weird random sample insertion)
    private void InsertRandomSpadinner(Vegas vegas, MediaEvent ev, string folder, Random rng)
    {
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;
        var files = Directory.GetFiles(folder);
        if (files.Length == 0) return;
        var pick = files[rng.Next(files.Length)];
        // Insert onto an audio track near the event position
    }

    #endregion

    #region Helper placeholder methods (adapt API specifics here)
    private bool IsAudioFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return new[] { ".wav", ".mp3", ".aif", ".aiff", ".flac", ".m4a" }.Contains(ext);
    }

    // Duplicate an event - placeholder: real API calls differ
    private MediaEvent DuplicateEvent(MediaEvent original, Timecode desiredLen)
    {
        // Implementation depends on API: one approach is to copy the media reference and create a new event on a track
        // This placeholder returns null — adapt this with Vegas.AddEvent or similar methods
        return null;
    }

    private void InsertEventAt(Vegas vegas, MediaEvent ev, Timecode start)
    {
        // API to insert event at specific time on a track:
        // Track t = FindAppropriateTrack(vegas, ev);
        // t.AddEvent(ev.Media, start, ev.Length);
    }

    private MediaEvent SafeExtractSlice(Vegas vegas, MediaEvent ev, Timecode start, Timecode end)
    {
        // Splits will typically require splitting the event at start and end; use Event.Split API carefully.
        // Return a new media event representing the slice for further manipulation.
        return null;
    }
    #endregion
}