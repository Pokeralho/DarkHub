using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DarkHub
{
    public class PasswordEntry
    {
        public string Category { get; set; }
        public DateTime CreatedDate { get; set; }
        public string EncryptedPassword { get; set; }
        public int Id { get; set; }
        public string Password { get; set; }
        public string Title { get; set; }
    }

    public partial class PasswordManager : Page
    {
        private const string DbPath = "passwords.db";
        private const int InactivityTimeout = 300;
        private const string MasterHashPath = "master.hash";
        private const string MasterKeyIdPath = "masterkey.id";

        private static readonly string[] WordList = new[]
{
    "abandon", "ability", "able", "about", "above", "absent", "absorb", "abstract", "absurd", "abuse",
    "access", "accident", "account", "accuse", "achieve", "acid", "acoustic", "acquire", "across", "act",
    "action", "actor", "actress", "actual", "adapt", "add", "addict", "address", "adjust", "admit",
    "adult", "advance", "advice", "aerobic", "affair", "afford", "afraid", "again", "age", "agent",
    "agree", "ahead", "aim", "air", "airport", "aisle", "alarm", "album", "alcohol", "alert",
    "alien", "all", "alley", "allow", "almost", "alone", "alpha", "already", "also", "alter",
    "always", "amateur", "amazing", "among", "amount", "amused", "analyst", "anchor", "ancient", "anger",
    "angle", "angry", "animal", "ankle", "announce", "annual", "another", "answer", "antenna", "antique",
    "anxiety", "any", "apart", "apology", "appear", "apple", "approve", "april", "arch", "arctic",
    "area", "arena", "argue", "arm", "armed", "armor", "army", "around", "arrange", "arrest",
    "arrive", "arrow", "art", "artefact", "artist", "artwork", "ask", "aspect", "assault", "asset",
    "assist", "assume", "asthma", "athlete", "atom", "attack", "attend", "attitude", "attract", "auction",
    "audit", "august", "aunt", "author", "auto", "autumn", "average", "avocado", "avoid", "awake",
    "aware", "away", "awesome", "awful", "awkward", "axis", "baby", "bachelor", "bacon", "badge",
    "bag", "balance", "balcony", "ball", "bamboo", "banana", "banner", "bar", "barely", "bargain",
    "barrel", "base", "basic", "basket", "battle", "beach", "bean", "beauty", "because", "become",
    "beef", "before", "begin", "behave", "behind", "believe", "below", "belt", "bench", "benefit",
    "best", "betray", "better", "between", "beyond", "bicycle", "bid", "bike", "bind", "biology",
    "bird", "birth", "bitter", "black", "blade", "blame", "blanket", "blast", "bleak", "bless",
    "blind", "blood", "blossom", "blouse", "blue", "blur", "blush", "board", "boat", "body",
    "boil", "bomb", "bone", "bonus", "book", "boost", "border", "boring", "borrow", "boss",
    "bottom", "bounce", "box", "boy", "bracket", "brain", "brand", "brass", "brave", "bread",
    "breeze", "brick", "bridge", "brief", "bright", "bring", "brisk", "broccoli", "broken", "bronze",
    "broom", "brother", "brown", "brush", "bubble", "buddy", "budget", "buffalo", "build", "bulb",
    "bulk", "bullet", "bundle", "bunker", "burden", "burger", "burst", "bus", "business", "busy",
    "butter", "buyer", "buzz", "cabbage", "cabin", "cable", "cactus", "cage", "cake", "call",
    "calm", "camera", "camp", "can", "canal", "cancel", "candy", "cannon", "canoe", "canvas",
    "canyon", "capable", "capital", "captain", "car", "carbon", "card", "cargo", "carpet", "carry",
    "cart", "case", "cash", "casino", "castle", "casual", "cat", "catalog", "catch", "category",
    "cattle", "caught", "cause", "caution", "cave", "ceiling", "celery", "cement", "census", "century",
    "cereal", "certain", "chair", "chalk", "champion", "change", "chaos", "chapter", "charge", "chase",
    "chat", "cheap", "check", "cheese", "chef", "cherry", "chest", "chicken", "chief", "child",
    "chimney", "choice", "choose", "chronic", "chuckle", "chunk", "churn", "cigar", "cinnamon", "circle",
    "citizen", "city", "civil", "claim", "clap", "clarify", "claw", "clay", "clean", "clerk",
    "clever", "click", "client", "cliff", "climb", "clinic", "clip", "clock", "clog", "close",
    "cloth", "cloud", "clown", "club", "clump", "cluster", "clutch", "coach", "coast", "coconut",
    "code", "coffee", "coil", "coin", "collect", "color", "column", "combine", "come", "comfort",
    "comic", "common", "company", "concert", "conduct", "confirm", "congress", "connect", "consider", "control",
    "convince", "cook", "cool", "copper", "copy", "coral", "core", "corn", "correct", "cost",
    "cotton", "couch", "country", "couple", "course", "cousin", "cover", "coyote", "crack", "cradle",
    "craft", "cram", "crane", "crash", "crater", "crawl", "crazy", "cream", "credit", "creek",
    "crew", "cricket", "crime", "crisp", "critic", "crop", "cross", "crouch", "crowd", "crucial",
    "cruel", "cruise", "crumble", "crunch", "crush", "cry", "crystal", "cube", "culture", "cup",
    "cupboard", "curious", "current", "curtain", "curve", "cushion", "custom", "cute", "cycle", "dad",
    "damage", "damp", "dance", "danger", "daring", "dash", "daughter", "dawn", "day", "deal",
    "debate", "debris", "decade", "december", "decide", "decline", "decorate", "decrease", "deer", "defense",
    "define", "defy", "degree", "delay", "deliver", "demand", "demise", "denial", "dentist", "deny",
    "depart", "depend", "deposit", "depth", "deputy", "derive", "describe", "desert", "design", "desk",
    "despair", "destroy", "detail", "detect", "develop", "device", "devote", "diagram", "dial", "diamond",
    "diary", "dice", "diesel", "diet", "differ", "digital", "dignity", "dilemma", "dinner", "dinosaur",
    "direct", "dirt", "disagree", "discover", "disease", "dish", "dismiss", "disorder", "display", "distance",
    "divert", "divide", "divorce", "dizzy", "doctor", "document", "dog", "doll", "dolphin", "domain",
    "donate", "donkey", "donor", "door", "dose", "double", "dove", "draft", "dragon", "drama",
    "drastic", "draw", "dream", "dress", "drift", "drill", "drink", "drip", "drive", "drop",
    "drum", "dry", "duck", "dumb", "dune", "during", "dust", "dutch", "duty", "dwarf",
    "dynamic", "eager", "eagle", "early", "earn", "earth", "easily", "east", "easy", "echo",
    "ecology", "economy", "edge", "edit", "educate", "effort", "egg", "eight", "either", "elbow",
    "elder", "electric", "elegant", "element", "elephant", "elevator", "elite", "else", "embark", "embody",
    "embrace", "emerge", "emotion", "employ", "empower", "empty", "enable", "enact", "end", "endless",
    "endorse", "enemy", "energy", "enforce", "engage", "engine", "enhance", "enjoy", "enlist", "enough",
    "enrich", "enroll", "ensure", "enter", "entire", "entry", "envelope", "episode", "equal", "equip",
    "era", "erase", "erode", "erosion", "error", "erupt", "escape", "essay", "essence", "estate",
    "eternal", "ethics", "evidence", "evil", "evoke", "evolve", "exact", "example", "excess", "exchange",
    "excite", "exclude", "excuse", "execute", "exercise", "exhaust", "exhibit", "exile", "exist", "exit",
    "exotic", "expand", "expect", "expire", "explain", "expose", "express", "extend", "extra", "eye",
    "eyebrow", "fabric", "face", "faculty", "fade", "faint", "faith", "fall", "false", "fame",
    "family", "famous", "fan", "fancy", "fantasy", "farm", "fashion", "fat", "fatal", "father",
    "fatigue", "fault", "favorite", "feature", "february", "federal", "fee", "feed", "feel", "female",
    "fence", "festival", "fetch", "fever", "few", "fiber", "fiction", "field", "figure", "file",
    "film", "filter", "final", "find", "fine", "finger", "finish", "fire", "firm", "first",
    "fiscal", "fish", "fit", "fitness", "fix", "flag", "flame", "flash", "flat", "flavor",
    "flee", "flight", "flip", "float", "flock", "floor", "flower", "fluid", "flush", "fly",
    "foam", "focus", "fog", "foil", "fold", "follow", "food", "foot", "force", "forest",
    "forget", "fork", "fortune", "forum", "forward", "fossil", "foster", "found", "fox", "fragile",
    "frame", "frequent", "fresh", "friend", "fringe", "frog", "front", "frost", "frown", "frozen",
    "fruit", "fuel", "fun", "funny", "furnace", "fury", "future", "gadget", "gain", "galaxy",
    "gallery", "game", "gap", "garage", "garbage", "garden", "garlic", "garment", "gas", "gasp",
    "gate", "gather", "gauge", "gaze", "general", "genius", "genre", "gentle", "genuine", "gesture",
    "ghost", "giant", "gift", "giggle", "ginger", "giraffe", "girl", "give", "glad", "glance",
    "glare", "glass", "glide", "glimpse", "globe", "gloom", "glory", "glove", "glow", "glue",
    "goat", "goddess", "gold", "good", "goose", "gorilla", "gospel", "gossip", "govern", "gown",
    "grab", "grace", "grain", "grant", "grape", "grass", "gravity", "great", "green", "grid",
    "grief", "grill", "grin", "grip", "grocery", "group", "grow", "grunt", "guard", "guess",
    "guide", "guilt", "guitar", "gun", "gym", "habit", "hair", "half", "hammer", "hamster",
    "hand", "happy", "harbor", "hard", "harsh", "harvest", "hat", "have", "hawk", "hazard",
    "head", "health", "heart", "heavy", "hedgehog", "height", "hello", "helmet", "help", "hen",
    "hero", "hidden", "high", "hill", "hint", "hip", "hire", "history", "hit", "hobby",
    "hockey", "hold", "hole", "holiday", "hollow", "home", "honey", "hood", "hope", "horn",
    "horror", "horse", "hospital", "host", "hotel", "hour", "hover", "hub", "huge", "human",
    "humble", "humor", "hundred", "hungry", "hunt", "hurdle", "hurry", "hurt", "husband", "hybrid",
    "ice", "icon", "idea", "identify", "idle", "ignore", "ill", "illegal", "illness", "image",
    "imitate", "immense", "immune", "impact", "impose", "improve", "impulse", "inch", "include", "income",
    "increase", "index", "indicate", "indoor", "industry", "infant", "inflict", "informфей", "inhale", "inherit",
    "initial", "inject", "injury", "inmate", "inner", "innocent", "input", "inquiry", "insane", "insect",
    "inside", "inspire", "install", "intact", "interest", "into", "invest", "invite", "involve", "iron",
    "island", "isolate", "issue", "item", "ivory", "jacket", "jaguar", "jar", "jazz", "jealous",
    "jeans", "jelly", "jewel", "job", "join", "joke", "journey", "joy", "judge", "juice",
    "jump", "jungle", "junior", "junk", "just", "kangaroo", "keen", "keep", "ketchup", "key",
    "kick", "kid", "kidney", "kind", "kingdom", "kiss", "kit", "kitchen", "kite", "kitten",
    "kiwi", "knee", "knife", "knock", "know", "lab", "label", "labor", "ladder", "lady",
    "lake", "lamp", "language", "laptop", "large", "later", "latin", "laugh", "laundry", "lava",
    "law", "lawn", "lawsuit", "layer", "lazy", "leader", "leaf", "learn", "leave", "lecture",
    "left", "leg", "legal", "legend", "leisure", "lemon", "lend", "length", "lens", "leopard",
    "lesson", "letter", "level", "liar", "liberty", "library", "license", "life", "lift", "light",
    "like", "limb", "limit", "link", "lion", "liquid", "list", "little", "live", "lizard",
    "load", "loan", "lobster", "local", "lock", "logic", "lonely", "long", "loop", "lottery",
    "loud", "lounge", "love", "loyal", "lucky", "luggage", "lumber", "lunar", "lunch", "luxury",
    "lyrics", "machine", "mad", "magic", "magnet", "maid", "mail", "main", "major", "make",
    "mammal", "man", "manage", "mandate", "mango", "mansion", "manual", "maple", "marble", "march",
    "margin", "marine", "market", "marriage", "mask", "mass", "master", "match", "material", "math",
    "matrix", "matter", "maximum", "maze", "meadow", "mean", "measure", "meat", "mechanic", "medal",
    "media", "melody", "melt", "member", "memory", "mention", "menu", "mercy", "merge", "merit",
    "merry", "mesh", "message", "metal", "method", "middle", "midnight", "milk", "million", "mimic",
    "mind", "minimum", "minor", "minute", "miracle", "mirror", "misery", "miss", "mistake", "mix",
    "mixed", "mixture", "mobile", "model", "modify", "mom", "moment", "monitor", "monkey", "monster",
    "month", "moon", "moral", "more", "morning", "mosquito", "mother", "motion", "motor", "mountain",
    "mouse", "move", "movie", "much", "muffin", "mule", "multiply", "muscle", "museum", "mushroom",
    "music", "must", "mutual", "myself", "mystery", "myth", "naive", "name", "napkin", "narrow",
    "nasty", "nation", "nature", "near", "neck", "need", "negative", "neglect", "neither", "nephew",
    "nerve", "nest", "net", "network", "neutral", "never", "news", "next", "nice", "night",
    "noble", "noise", "nominee", "noodle", "normal", "north", "nose", "notable", "note", "nothing",
    "notice", "novel", "now", "nuclear", "number", "nurse", "nut", "oak", "obey", "object",
    "oblige", "obscure", "observe", "obtain", "obvious", "occur", "ocean", "october", "odor", "off",
    "offer", "office", "often", "oil", "okay", "old", "olive", "olympic", "omit", "once",
    "one", "onion", "online", "only", "open", "opera", "opinion", "oppose", "option", "orange",
    "orbit", "orchard", "order", "ordinary", "organ", "orient", "original", "orphan", "ostrich", "other",
    "outdoor", "outer", "output", "outside", "oval", "oven", "over", "own", "owner", "oxygen",
    "oyster", "ozone", "pact", "paddle", "page", "pair", "palace", "palm", "panda", "panel",
    "panic", "panther", "paper", "parade", "parent", "park", "parrot", "party", "pass", "patch",
    "path", "patient", "patrol", "pattern", "pause", "pave", "payment", "peace", "peanut", "pear",
    "peasant", "pelican", "pen", "penalty", "pencil", "people", "pepper", "perfect", "permit", "person",
    "pet", "phone", "photo", "phrase", "physical", "piano", "picnic", "picture", "piece", "pig",
    "pigeon", "pill", "pilot", "pink", "pioneer", "pipe", "pistol", "pitch", "pizza", "place",
    "planet", "plastic", "plate", "play", "please", "pledge", "pluck", "plug", "plunge", "poem",
    "poet", "point", "polar", "pole", "police", "pond", "pony", "pool", "popular", "portion",
    "position", "possible", "post", "potato", "pottery", "poverty", "powder", "power", "practice", "praise",
    "predict", "prefer", "prepare", "present", "pretty", "prevent", "price", "pride", "primary", "print",
    "priority", "prison", "private", "prize", "problem", "process", "produce", "profit", "program", "project",
    "promote", "proof", "property", "prosper", "protect", "proud", "provide", "public", "pudding", "pull",
    "pulp", "pulse", "pumpkin", "punch", "pupil", "puppy", "purchase", "purity", "purpose", "purse",
    "push", "put", "puzzle", "pyramid", "quality", "quantum", "quarter", "question", "quick", "quit",
    "quiz", "quote", "rabbit", "raccoon", "race", "rack", "radar", "radio", "rail", "rain",
    "raise", "rally", "ramp", "ranch", "random", "range", "rapid", "rare", "rate", "rather",
    "raven", "raw", "razor", "ready", "real", "reason", "rebel", "rebuild", "recall", "receive",
    "recipe", "record", "recycle", "reduce", "reflect", "reform", "refuse", "region", "regret", "regular",
    "reject", "relax", "release", "relief", "rely", "remain", "remember", "remind", "remove", "render",
    "renew", "rent", "reopen", "repair", "repeat", "replace", "report", "require", "rescue", "resemble",
    "resist", "resource", "response", "result", "retire", "retreat", "return", "reunion", "reveal", "review",
    "reward", "rhythm", "rib", "ribbon", "rice", "rich", "ride", "ridge", "rifle", "right",
    "rigid", "ring", "riot", "ripple", "risk", "ritual", "rival", "river", "road", "roast",
    "robot", "robust", "rocket", "romance", "roof", "rookie", "room", "rose", "rotate", "rough",
    "round", "route", "royal", "rubber", "rude", "rug", "rule", "run", "runway", "rural",
    "sad", "saddle", "sadness", "safe", "sail", "salad", "salmon", "salon", "salt", "salute",
    "same", "sample", "sand", "satisfy", "satoshi", "sauce", "sausage", "save", "say", "scale",
    "scan", "scare", "scatter", "scene", "scheme", "school", "science", "scissors", "scorpion", "scout",
    "scrap", "screen", "script", "scrub", "sea", "search", "season", "seat", "second", "secret",
    "section", "security", "seed", "seek", "segment", "select", "sell", "seminar", "senior", "sense",
    "sentence", "series", "service", "session", "settle", "setup", "seven", "shadow", "shaft", "shallow",
    "share", "shed", "shell", "sheriff", "shield", "shift", "shine", "ship", "shiver", "shock",
    "shoe", "shoot", "shop", "short", "shoulder", "shove", "shrimp", "shrug", "shuffle", "shy",
    "sibling", "sick", "side", "siege", "sight", "sign", "silent", "silk", "silly", "silver",
    "similar", "simple", "since", "sing", "siren", "sister", "sit", "six", "size", "skate",
    "sketch", "ski", "skill", "skin", "skirt", "skull", "slab", "slam", "sleep", "slender",
    "slice", "slide", "slight", "slim", "slogan", "slot", "slow", "slush", "small", "smart",
    "smile", "smoke", "smooth", "snack", "snake", "snap", "sniff", "snow", "soap", "soccer",
    "social", "sock", "soda", "soft", "solar", "soldier", "solid", "solution", "solve", "someone",
    "song", "soon", "sorry", "sort", "soul", "sound", "soup", "source", "south", "space",
    "spare", "spatial", "spawn", "speak", "special", "speed", "spell", "spend", "sphere", "spice",
    "spider", "spike", "spin", "spirit", "split", "spoil", "sponsor", "spoon", "sport", "spot",
    "spray", "spread", "spring", "spy", "square", "squeeze", "squirrel", "stable", "stadium", "staff",
    "stage", "stairs", "stamp", "stand", "start", "state", "stay", "steak", "steel", "stem",
    "step", "stereo", "stick", "still", "sting", "stock", "stomach", "stone", "stool", "story",
    "stove", "strategy", "street", "strike", "strong", "struggle", "student", "stuff", "stumble", "style",
    "subject", "submit", "subway", "success", "such", "sudden", "suffer", "sugar", "suggest", "suit",
    "summer", "sun", "sunny", "sunset", "super", "supply", "supreme", "sure", "surface", "surge",
    "surprise", "surround", "survey", "suspect", "sustain", "swallow", "swamp", "swap", "swarm", "swear",
    "sweet", "swift", "swim", "swing", "switch", "sword", "symbol", "symptom", "syrup", "system",
    "table", "tackle", "tag", "tail", "talent", "talk", "tank", "tape", "target", "task",
    "taste", "tattoo", "taxi", "teach", "team", "tear", "tease", "tedious", "teen", "telescope",
    "tell", "temper", "tenant", "tend", "tent", "term", "test", "text", "thank", "that",
    "theme", "then", "theory", "there", "they", "thing", "this", "thought", "three", "thrive",
    "throw", "thumb", "thunder", "ticket", "tide", "tiger", "tilt", "timber", "time", "tiny",
    "tip", "tired", "tissue", "title", "toast", "tobacco", "today", "toddler", "toe", "together",
    "toilet", "token", "tomato", "tomorrow", "tone", "tongue", "tonight", "tool", "tooth", "top",
    "topic", "topple", "torch", "tornado", "tortoise", "toss", "total", "tourist", "toward", "tower",
    "town", "toy", "track", "trade", "traffic", "tragic", "train", "transfer", "trap", "trash",
    "travel", "tray", "treat", "tree", "trend", "trial", "tribe", "trick", "trigger", "trim",
    "trip", "trophy", "trouble", "truck", "true", "truly", "trumpet", "trust", "truth", "try",
    "tube", "tuition", "tumble", "tuna", "tunnel", "turkey", "turn", "turtle", "twelve", "twenty",
    "twice", "twin", "twist", "two", "type", "typical", "ugly", "umbrella", "unable", "unaware",
    "uncle", "uncover", "under", "undo", "unfair", "unfold", "unhappy", "uniform", "unique", "unit",
    "universe", "unknown", "unlock", "until", "unusual", "unveil", "update", "upgrade", "uphold", "upon",
    "upper", "upset", "urban", "urge", "usage", "use", "used", "useful", "useless", "usual",
    "utility", "vacant", "vacuum", "vague", "valid", "valley", "valve", "van", "vanish", "vapor",
    "various", "vast", "vault", "vehicle", "velvet", "vendor", "venture", "venue", "verb", "verify",
    "version", "very", "vessel", "veteran", "viable", "vibrant", "vicious", "victory", "video", "view",
    "village", "vintage", "violin", "virtual", "virus", "visa", "visit", "visual", "vital", "vivid",
    "vocal", "voice", "void", "volcano", "volume", "vote", "voyage", "wage", "wagon", "wait",
    "walk", "wall", "walnut", "want", "warfare", "warm", "warrior", "wash", "wasp", "waste",
    "water", "wave", "way", "wealth", "weapon", "wear", "weasel", "weather", "web", "wedding",
    "weekend", "weird", "welcome", "west", "wet", "whale", "what", "wheat", "wheel", "when",
    "where", "whip", "whisper", "wide", "width", "wife", "wild", "will", "win", "window",
    "wine", "wing", "wink", "winner", "winter", "wire", "wisdom", "wise", "wish", "witness",
    "wolf", "woman", "wonder", "wood", "wool", "word", "work", "world", "worry", "worth",
    "wrap", "wreck", "wrestle", "wrist", "write", "wrong", "yard", "year", "yellow", "you",
    "young", "youth", "zebra", "zero", "zone", "zoo"
};

        private TextBox confirmPasswordTextBox;
        private bool isConfirmPasswordVisible = false;
        private bool isMasterPasswordVisible = false;
        private bool isNewPasswordVisible = false;

        private DateTime LastActivity;

        private string MasterKey;
        private TextBox masterPasswordTextBox;
        private TextBox newPasswordTextBox;
        private string[] RecoveryWords;

        public PasswordManager()
        {
            InitializeComponent();
            SetupDatabase();
            CheckFirstRun();
            LastActivity = DateTime.Now;
            CompositionTarget.Rendering += CheckInactivity;
            InitializePasswordTextBoxes();
        }

        private Style CreateButtonStyle()
        {
            var style = new Style(typeof(Button));

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));

            var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentPresenterFactory);

            template.VisualTree = borderFactory;

            var trigger = new Trigger
            {
                Property = Button.IsMouseOverProperty,
                Value = true
            };
            trigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0, 168, 154))));

            template.Triggers.Add(trigger);

            style.Setters.Add(new Setter(Button.TemplateProperty, template));

            return style;
        }

        private void InitializePasswordTextBoxes()
        {
            newPasswordTextBox = NewMasterPasswordTextBox;
            confirmPasswordTextBox = ConfirmMasterPasswordTextBox;
            masterPasswordTextBox = MasterPasswordTextBox;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            LastActivity = DateTime.Now;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            LastActivity = DateTime.Now;
        }

        private void Authenticate()
        {
            string inputPassword = isMasterPasswordVisible ? MasterPasswordTextBox.Text : MasterPasswordBox.Password;
            string storedHash;

            try
            {
                storedHash = File.ReadAllText(MasterHashPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao ler a senha mestra: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (BCrypt.Net.BCrypt.Verify(inputPassword, storedHash))
                {
                    MasterKey = DeriveKey(inputPassword);
                    string computedMasterKeyId = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(MasterKey)));
                    string storedMasterKeyId = GetStoredMasterKeyId();

                    if (storedMasterKeyId != null && computedMasterKeyId != storedMasterKeyId)
                    {
                        MessageBox.Show("A senha mestra é válida, mas os dados do banco não correspondem a esta chave. Possível comprometimento.",
                            "Erro de Integridade", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    LoginPanel.Visibility = Visibility.Collapsed;
                    MainPanel.Visibility = Visibility.Visible;
                    LoadPasswords();
                }
                else
                {
                    MessageBox.Show(ResourceManagerHelper.Instance.Senhamestraincorreta32, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (BCrypt.Net.SaltParseException)
            {
                File.Delete(MasterHashPath);
                RegisterPanel.Visibility = Visibility.Visible;
                LoginPanel.Visibility = Visibility.Collapsed;
                MessageBox.Show(ResourceManagerHelper.Instance.Oformatodasenhamestr33, "Erro de Formato", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Backup de Senhas (*.bak)|*.bak", FileName = "passwords_backup.bak" };
            if (dialog.ShowDialog() == true)
            {
                File.Copy(DbPath, dialog.FileName, true);
                MessageBox.Show(ResourceManagerHelper.Instance.Backupconcluído34, "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool HasDatabaseData()
        {
            if (!File.Exists(DbPath)) return false;
            using var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            conn.Open();
            string sql = "SELECT COUNT(*) FROM Passwords";
            using var cmd = new SQLiteCommand(sql, conn);
            long count = (long)cmd.ExecuteScalar();
            return count > 0;
        }

        private string GetStoredMasterKeyId()
        {
            if (File.Exists(MasterKeyIdPath))
                return File.ReadAllText(MasterKeyIdPath);
            return null;
        }

        private void CheckFirstRun()
        {
            bool masterHashExists = File.Exists(MasterHashPath);
            bool masterKeyIdExists = File.Exists(MasterKeyIdPath);
            bool dbHasData = HasDatabaseData();

            if (!masterHashExists && !dbHasData && !masterKeyIdExists)
            {
                RegisterPanel.Visibility = Visibility.Visible;
                LoginPanel.Visibility = Visibility.Collapsed;
            }
            else if (!masterHashExists && (dbHasData || masterKeyIdExists))
            {
                RegisterPanel.Visibility = Visibility.Collapsed;
                LoginPanel.Visibility = Visibility.Visible;
                MessageBox.Show("O arquivo de senha mestra foi comprometido. Use as palavras de recuperação para restaurar o acesso.",
                    "Segurança Comprometida", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                string storedHash = File.ReadAllText(MasterHashPath);
                if (!storedHash.StartsWith("$2"))
                {
                    File.Delete(MasterHashPath);
                    RegisterPanel.Visibility = Visibility.Visible;
                    LoginPanel.Visibility = Visibility.Collapsed;
                    MessageBox.Show(ResourceManagerHelper.Instance.Detectamosumformatod35,
                        "Atualização Necessária", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    RegisterPanel.Visibility = Visibility.Collapsed;
                    LoginPanel.Visibility = Visibility.Visible;
                }
            }
        }

        private void CheckInactivity(object sender, EventArgs e)
        {
            if (MainPanel.Visibility == Visibility.Visible && (DateTime.Now - LastActivity).TotalSeconds > InactivityTimeout)
            {
                LogoutButton_Click(null, null);
                MessageBox.Show(ResourceManagerHelper.Instance.Sessãobloqueadaporin36, "Bloqueio", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ConfirmMasterPasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) RegisterMasterPassword();
        }

        private void CopyPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var entry = PasswordsGrid.Items.Cast<PasswordEntry>().First(p => p.Id == id);
                string decryptedPassword = Decrypt(entry.EncryptedPassword, MasterKey);
                Clipboard.SetText(decryptedPassword);

                var window = new Window
                {
                    Title = ResourceManagerHelper.Instance.copypw,
                    Width = 350,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64)),
                    BorderThickness = new Thickness(1),
                    ResizeMode = ResizeMode.NoResize
                };

                var grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var titleLabel = new TextBlock
                {
                    Text = ResourceManagerHelper.Instance.SenhaCopiada1,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    Margin = new Thickness(0, 0, 0, 20),
                    TextAlignment = TextAlignment.Center
                };
                Grid.SetRow(titleLabel, 0);
                grid.Children.Add(titleLabel);

                var messageLabel = new TextBlock
                {
                    Text = ResourceManagerHelper.Instance.Asenhafoicopiadapara2,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    Margin = new Thickness(0, 0, 0, 15),
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetRow(messageLabel, 1);
                grid.Children.Add(messageLabel);

                var closeButton = new Button
                {
                    Content = ResourceManagerHelper.Instance.Fechar3,
                    Width = 150,
                    Height = 40,
                    Background = new SolidColorBrush(Color.FromRgb(0, 196, 180)),
                    Foreground = Brushes.White,
                    FontSize = 14,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                closeButton.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
                closeButton.Style = CreateButtonStyle();
                Grid.SetRow(closeButton, 2);
                grid.Children.Add(closeButton);

                window.Content = grid;

                closeButton.Click += (s, ev) => window.Close();

                window.ShowDialog();
            }
        }

        private string Decrypt(string cipherText, string key)
        {
            byte[] buffer = Convert.FromBase64String(cipherText);
            byte[] iv = buffer.Take(16).ToArray();
            byte[] cipher = buffer.Skip(16).ToArray();
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = Convert.FromBase64String(key);
            aes.IV = iv;
            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipher);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }

        private void DeletePassword(int id)
        {
            using var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            conn.Open();
            string sql = "DELETE FROM Passwords WHERE Id = @id";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private void DeletePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var entry = PasswordsGrid.Items.Cast<PasswordEntry>().First(p => p.Id == id);

                var window = new Window
                {
                    Title = "Excluir Senha",
                    Width = 350,
                    Height = 250,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64)),
                    BorderThickness = new Thickness(1),
                    ResizeMode = ResizeMode.NoResize
                };

                var grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var titleLabel = new TextBlock
                {
                    Text = ResourceManagerHelper.Instance.ConfirmarExclusão4,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    Margin = new Thickness(0, 0, 0, 20),
                    TextAlignment = TextAlignment.Center
                };
                Grid.SetRow(titleLabel, 0);
                grid.Children.Add(titleLabel);

                var messageLabel = new TextBlock
                {
                    Text = $"Deseja excluir a senha '{entry.Title}'?\nEsta ação é irreversível.",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    Margin = new Thickness(0, 0, 0, 15),
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetRow(messageLabel, 1);
                grid.Children.Add(messageLabel);

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(buttonPanel, 2);
                grid.Children.Add(buttonPanel);

                var confirmButton = new Button
                {
                    Content = ResourceManagerHelper.Instance.Confirmar5,
                    Width = 100,
                    Height = 40,
                    Background = new SolidColorBrush(Color.FromRgb(0, 196, 180)),
                    Foreground = Brushes.White,
                    FontSize = 14,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                confirmButton.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
                confirmButton.Style = CreateButtonStyle();
                buttonPanel.Children.Add(confirmButton);

                var cancelButton = new Button
                {
                    Content = ResourceManagerHelper.Instance.Cancelar6,
                    Width = 100,
                    Height = 40,
                    Background = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                    Foreground = Brushes.White,
                    FontSize = 14,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                cancelButton.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
                cancelButton.Style = CreateButtonStyle();
                buttonPanel.Children.Add(cancelButton);

                window.Content = grid;

                confirmButton.Click += (s, ev) =>
                {
                    DeletePassword(id);
                    LoadPasswords();
                    window.Close();

                    var successWindow = new Window
                    {
                        Title = "Exclusão Concluída",
                        Width = 350,
                        Height = 180,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64)),
                        BorderThickness = new Thickness(1),
                        ResizeMode = ResizeMode.NoResize
                    };

                    var successGrid = new Grid { Margin = new Thickness(20) };
                    successGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    successGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var successLabel = new TextBlock
                    {
                        Text = ResourceManagerHelper.Instance.Senhaexcluídacomsuce7,
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                        Margin = new Thickness(0, 0, 0, 20),
                        TextAlignment = TextAlignment.Center
                    };
                    Grid.SetRow(successLabel, 0);
                    successGrid.Children.Add(successLabel);

                    var okButton = new Button
                    {
                        Content = ResourceManagerHelper.Instance.OK8,
                        Width = 100,
                        Height = 40,
                        Background = new SolidColorBrush(Color.FromRgb(0, 196, 180)),
                        Foreground = Brushes.White,
                        FontSize = 14,
                        BorderThickness = new Thickness(0),
                        Cursor = Cursors.Hand,
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    okButton.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
                    okButton.Style = CreateButtonStyle();
                    Grid.SetRow(okButton, 1);
                    successGrid.Children.Add(okButton);

                    successWindow.Content = successGrid;

                    okButton.Click += (s2, ev2) => successWindow.Close();

                    successWindow.ShowDialog();
                };

                cancelButton.Click += (s, ev) => window.Close();

                window.ShowDialog();
            }
        }

        private string DeriveKey(string password)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("DarkHubSalt"), 10000, HashAlgorithmName.SHA256);
            return Convert.ToBase64String(pbkdf2.GetBytes(32));
        }

        private string DerivePasswordFromRecoveryWords(string[] words)
        {
            string seed = string.Join("", words);
            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(seed));
            return Convert.ToBase64String(hash).Substring(0, 12);
        }

        private void EditPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var entry = PasswordsGrid.Items.Cast<PasswordEntry>().First(p => p.Id == id);
                var window = new Window
                {
                    Title = "Editar Senha",
                    Width = 350,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64)),
                    BorderThickness = new Thickness(1),
                    ResizeMode = ResizeMode.NoResize
                };

                var grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var titleLabel = new TextBlock
                {
                    Text = ResourceManagerHelper.Instance.EditarSenha9,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    Margin = new Thickness(0, 0, 0, 20),
                    TextAlignment = TextAlignment.Center
                };
                Grid.SetRow(titleLabel, 0);
                grid.Children.Add(titleLabel);

                var titleBox = new TextBox
                {
                    Width = 300,
                    Height = 40,
                    FontSize = 14,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 15),
                    Text = string.IsNullOrWhiteSpace(entry.Title) ? ResourceManagerHelper.Instance.Título10 : entry.Title,
                    Foreground = string.IsNullOrWhiteSpace(entry.Title) ? new SolidColorBrush(Color.FromRgb(160, 160, 160)) : new SolidColorBrush(Color.FromRgb(224, 224, 224))
                };
                titleBox.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
                titleBox.GotFocus += (s, ev) => { if (titleBox.Text == ResourceManagerHelper.Instance.Título10) { titleBox.Text = ""; titleBox.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)); } };
                titleBox.LostFocus += (s, ev) => { if (string.IsNullOrWhiteSpace(titleBox.Text)) { titleBox.Text = ResourceManagerHelper.Instance.Título10; titleBox.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)); } };
                Grid.SetRow(titleBox, 1);
                grid.Children.Add(titleBox);

                var categoryBox = new TextBox
                {
                    Width = 300,
                    Height = 40,
                    FontSize = 14,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 15),
                    Text = string.IsNullOrWhiteSpace(entry.Category) ? ResourceManagerHelper.Instance.Categoria11 : entry.Category,
                    Foreground = string.IsNullOrWhiteSpace(entry.Category) ? new SolidColorBrush(Color.FromRgb(160, 160, 160)) : new SolidColorBrush(Color.FromRgb(224, 224, 224))
                };
                categoryBox.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
                categoryBox.GotFocus += (s, ev) => { if (categoryBox.Text == ResourceManagerHelper.Instance.Categoria11) { categoryBox.Text = ""; categoryBox.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)); } };
                categoryBox.LostFocus += (s, ev) => { if (string.IsNullOrWhiteSpace(categoryBox.Text)) { categoryBox.Text = ResourceManagerHelper.Instance.Categoria11; categoryBox.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)); } };
                Grid.SetRow(categoryBox, 2);
                grid.Children.Add(categoryBox);

                var saveButton = new Button
                {
                    Content = ResourceManagerHelper.Instance.Salvar12,
                    Width = 150,
                    Height = 40,
                    Background = new SolidColorBrush(Color.FromRgb(0, 196, 180)),
                    Foreground = Brushes.White,
                    FontSize = 14,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                saveButton.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
                saveButton.Style = CreateButtonStyle();
                Grid.SetRow(saveButton, 3);
                grid.Children.Add(saveButton);

                window.Content = grid;

                saveButton.Click += (s, ev) =>
                {
                    UpdatePassword(id, titleBox.Text, categoryBox.Text);
                    LoadPasswords();
                    window.Close();
                };

                window.ShowDialog();
            }
        }

        private string Encrypt(string plainText, string key)
        {
            byte[] iv = new byte[16];
            RandomNumberGenerator.Fill(iv);
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = Convert.FromBase64String(key);
            aes.IV = iv;
            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            ms.Write(iv, 0, iv.Length);
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        private void GeneratePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new Window
            {
                Title = ResourceManagerHelper.Instance.GerarNovaSenha13,
                Width = 350,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64)),
                BorderThickness = new Thickness(1),
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleLabel = new TextBlock
            {
                Text = ResourceManagerHelper.Instance.GerarNovaSenha13,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                Margin = new Thickness(0, 0, 0, 20),
                TextAlignment = TextAlignment.Center
            };
            Grid.SetRow(titleLabel, 0);
            grid.Children.Add(titleLabel);

            var titleBox = new TextBox
            {
                Width = 300,
                Height = 40,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 15),
                Text = ResourceManagerHelper.Instance.Título10,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160))
            };
            titleBox.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            titleBox.GotFocus += (s, ev) => { if (titleBox.Text == ResourceManagerHelper.Instance.Título10) { titleBox.Text = ""; titleBox.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)); } };
            titleBox.LostFocus += (s, ev) => { if (string.IsNullOrWhiteSpace(titleBox.Text)) { titleBox.Text = ResourceManagerHelper.Instance.Título10; titleBox.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)); } };
            Grid.SetRow(titleBox, 1);
            grid.Children.Add(titleBox);

            var categoryBox = new TextBox
            {
                Width = 300,
                Height = 40,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 15),
                Text = ResourceManagerHelper.Instance.Categoria11,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160))
            };
            categoryBox.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            categoryBox.GotFocus += (s, ev) => { if (categoryBox.Text == ResourceManagerHelper.Instance.Categoria11) { categoryBox.Text = ""; categoryBox.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)); } };
            categoryBox.LostFocus += (s, ev) => { if (string.IsNullOrWhiteSpace(categoryBox.Text)) { categoryBox.Text = ResourceManagerHelper.Instance.Categoria11; categoryBox.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)); } };
            Grid.SetRow(categoryBox, 2);
            grid.Children.Add(categoryBox);

            var lengthBox = new TextBox
            {
                Width = 300,
                Height = 40,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 15),
                Text = ResourceManagerHelper.Instance.Comprimentoex1618,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160))
            };
            lengthBox.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            lengthBox.GotFocus += (s, ev) => { if (lengthBox.Text == "Comprimento (ex: 16)") { lengthBox.Text = ""; lengthBox.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)); } };
            lengthBox.LostFocus += (s, ev) => { if (string.IsNullOrWhiteSpace(lengthBox.Text)) { lengthBox.Text = ResourceManagerHelper.Instance.Comprimentoex1618; lengthBox.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)); } };
            Grid.SetRow(lengthBox, 3);
            grid.Children.Add(lengthBox);

            var generateButton = new Button
            {
                Content = ResourceManagerHelper.Instance.GerareSalvar21,
                Width = 150,
                Height = 40,
                Background = new SolidColorBrush(Color.FromRgb(0, 196, 180)),
                Foreground = Brushes.White,
                FontSize = 14,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 0, 10)
            };
            generateButton.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            generateButton.Style = CreateButtonStyle();
            Grid.SetRow(generateButton, 4);
            grid.Children.Add(generateButton);

            window.Content = grid;

            generateButton.Click += (s, ev) =>
            {
                int length = int.TryParse(lengthBox.Text, out int l) ? l : 16;
                string password = GenerateRandomPassword(length);
                string encryptedPassword = Encrypt(password, MasterKey);
                SavePassword(titleBox.Text, categoryBox.Text, encryptedPassword);
                LoadPasswords();
                window.Close();
            };

            window.ShowDialog();
        }

        private void GenerateMasterPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string generatedPassword = GenerateRandomPassword(32);

            if (isNewPasswordVisible)
            {
                NewMasterPasswordTextBox.Text = generatedPassword;
            }
            else
            {
                NewMasterPasswordBox.Password = generatedPassword;
            }

            if (isConfirmPasswordVisible)
            {
                ConfirmMasterPasswordTextBox.Text = generatedPassword;
            }
            else
            {
                ConfirmMasterPasswordBox.Password = generatedPassword;
            }

            Clipboard.SetText(generatedPassword);

            string message = $"The generated master password is:\n\n{generatedPassword}\n\n" +
                             "It has been copied to the clipboard. " +
                             "This will be the password saved when you click on 'Register'.";
            MessageBox.Show(message, "Generated Password", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string[] GenerateRecoveryWords(int count)
        {
            var random = new Random();
            var selectedWords = new HashSet<string>();
            while (selectedWords.Count < count)
            {
                selectedWords.Add(WordList[random.Next(WordList.Length)]);
            }
            return selectedWords.ToArray();
        }

        private void LoadPasswords()
        {
            var passwords = new List<PasswordEntry>();
            using var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            conn.Open();
            string sql = "SELECT Id, Title, Category, Password, CreatedDate FROM Passwords";
            using var cmd = new SQLiteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                passwords.Add(new PasswordEntry
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Category = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Password = "••••••••",
                    EncryptedPassword = reader.GetString(3),
                    CreatedDate = DateTime.Parse(reader.GetString(4))
                });
            }
            PasswordsGrid.ItemsSource = passwords;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            Authenticate();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MasterKey = null;
            MainPanel.Visibility = Visibility.Collapsed;
            LoginPanel.Visibility = Visibility.Visible;
            MasterPasswordBox.Password = "";
            masterPasswordTextBox.Text = "";
            PasswordsGrid.ItemsSource = null;
        }

        private void MasterPasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Authenticate();
        }

        private void RecoverButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new Window
            {
                Title = "Recuperar Senha Mestra",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64)),
                BorderThickness = new Thickness(1),
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleLabel = new TextBlock
            {
                Text = ResourceManagerHelper.Instance.RecuperarSenhaMestra22,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                Margin = new Thickness(0, 0, 0, 20),
                TextAlignment = TextAlignment.Center
            };
            Grid.SetRow(titleLabel, 0);
            grid.Children.Add(titleLabel);

            var recoveryWordsBox = new TextBox
            {
                Width = 350,
                Height = 100,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 15),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap
            };
            recoveryWordsBox.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            Grid.SetRow(recoveryWordsBox, 1);
            grid.Children.Add(recoveryWordsBox);

            var recoverButton = new Button
            {
                Content = ResourceManagerHelper.Instance.Recuperar23,
                Width = 150,
                Height = 40,
                Background = new SolidColorBrush(Color.FromRgb(0, 196, 180)),
                Foreground = Brushes.White,
                FontSize = 14,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            recoverButton.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            recoverButton.Style = CreateButtonStyle();
            Grid.SetRow(recoverButton, 2);
            grid.Children.Add(recoverButton);

            window.Content = grid;

            recoverButton.Click += (s, ev) =>
            {
                string inputWords = recoveryWordsBox.Text.Trim();
                inputWords = System.Text.RegularExpressions.Regex.Replace(inputWords, @"\s+", " ");
                string[] words = inputWords.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (words.Length != 16)
                {
                    MessageBox.Show($"Você forneceu {words.Length} palavras, mas são esperadas exatamente 16. Verifique sua entrada.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string derivedPassword = DerivePasswordFromRecoveryWords(words);

                if (!Enumerable.SequenceEqual(words.OrderBy(w => w), RecoveryWords.OrderBy(w => w)))
                {
                    MessageBox.Show(ResourceManagerHelper.Instance.Aspalavrasderecupera39, ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Clipboard.SetText(derivedPassword);
                MessageBox.Show($"Recuperação bem-sucedida! A senha temporária derivada é:\n\n{derivedPassword}\n\n" +
                                "Ela foi copiada para a área de transferência. Use-a para fazer login e redefinir sua senha mestra.",
                                ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);

                MasterKey = DeriveKey(derivedPassword);
                LoginPanel.Visibility = Visibility.Collapsed;
                MainPanel.Visibility = Visibility.Visible;
                LoadPasswords();
                window.Close();
            };

            window.ShowDialog();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterMasterPassword();
        }

        private void RegisterMasterPassword()
        {
            string newPassword = isNewPasswordVisible ? NewMasterPasswordTextBox.Text : NewMasterPasswordBox.Password;
            string confirmPassword = isConfirmPasswordVisible ? ConfirmMasterPasswordTextBox.Text : ConfirmMasterPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                MessageBox.Show(ResourceManagerHelper.Instance.Asenhamestradeveterp40,
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show(ResourceManagerHelper.Instance.Assenhasnãocoincidem41,
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            File.WriteAllText(MasterHashPath, hashedPassword);

            MasterKey = DeriveKey(newPassword);

            string masterKeyId = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(MasterKey)));
            File.WriteAllText(MasterKeyIdPath, masterKeyId);

            RecoveryWords = GenerateRecoveryWords(16);
            string recoveryMessage = $"{ResourceManagerHelper.Instance.StorePassword}\n" +
                                     string.Join("\n", RecoveryWords) + "\n" +
                                     ResourceManagerHelper.Instance.RecoverPassword;
            var result = MessageBox.Show(recoveryMessage, ResourceManagerHelper.Instance.RecoveryWords,
                MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result == MessageBoxResult.OK)
            {
                Clipboard.SetText(string.Join(" ", RecoveryWords));
                MessageBox.Show(ResourceManagerHelper.Instance.Palavrasderecuperaçã42,
                    ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }

            RegisterPanel.Visibility = Visibility.Collapsed;
            MainPanel.Visibility = Visibility.Visible;
            LoadPasswords();
            MessageBox.Show(ResourceManagerHelper.Instance.Senhamestraregistrad43,
                ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Backup (*.bak)|*.bak" };
            if (dialog.ShowDialog() == true)
            {
                File.Copy(dialog.FileName, DbPath, true);
                LoadPasswords();
                MessageBox.Show(ResourceManagerHelper.Instance.Restauraçãoconcluída44, ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SavePassword(string title, string category, string encryptedPassword)
        {
            using var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            conn.Open();
            string sql = "INSERT INTO Passwords (Title, Category, Password, CreatedDate, MasterKeyId) VALUES (@title, @category, @password, @date, @masterKeyId)";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@category", category);
            cmd.Parameters.AddWithValue("@password", encryptedPassword);
            cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@masterKeyId", Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(MasterKey))));
            cmd.ExecuteNonQuery();
        }

        private void SetupDatabase()
        {
            if (!File.Exists(DbPath))
            {
                SQLiteConnection.CreateFile(DbPath);
                using var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
                conn.Open();
                string sql = @"CREATE TABLE Passwords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Category TEXT,
                Password TEXT NOT NULL,
                CreatedDate TEXT NOT NULL,
                MasterKeyId TEXT NOT NULL
            )";
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
        }

        private void ShowPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var entry = PasswordsGrid.Items.Cast<PasswordEntry>().First(p => p.Id == id);
                string decryptedPassword = Decrypt(entry.EncryptedPassword, MasterKey);

                var window = new Window
                {
                    Title = "Visualizar Senha",
                    Width = 350,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64)),
                    BorderThickness = new Thickness(1),
                    ResizeMode = ResizeMode.NoResize
                };

                var grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var titleLabel = new TextBlock
                {
                    Text = ResourceManagerHelper.Instance.SenhaDescriptografad24,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    Margin = new Thickness(0, 0, 0, 20),
                    TextAlignment = TextAlignment.Center
                };
                Grid.SetRow(titleLabel, 0);
                grid.Children.Add(titleLabel);

                var passwordBox = new TextBox
                {
                    Width = 300,
                    Height = 40,
                    FontSize = 14,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 15),
                    Text = decryptedPassword,
                    Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    IsReadOnly = true
                };
                passwordBox.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
                Grid.SetRow(passwordBox, 1);
                grid.Children.Add(passwordBox);

                var closeButton = new Button
                {
                    Content = ResourceManagerHelper.Instance.Fechar3,
                    Width = 150,
                    Height = 40,
                    Background = new SolidColorBrush(Color.FromRgb(0, 196, 180)),
                    Foreground = Brushes.White,
                    FontSize = 14,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                closeButton.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
                closeButton.Style = CreateButtonStyle();
                Grid.SetRow(closeButton, 2);
                grid.Children.Add(closeButton);

                window.Content = grid;

                closeButton.Click += (s, ev) =>
                {
                    entry.Password = "••••••••";
                    var items = PasswordsGrid.ItemsSource as List<PasswordEntry>;
                    if (items != null)
                    {
                        PasswordsGrid.ItemsSource = null;
                        PasswordsGrid.ItemsSource = items;
                    }
                    window.Close();
                };

                entry.Password = decryptedPassword;
                var itemsSource = PasswordsGrid.ItemsSource as List<PasswordEntry>;
                if (itemsSource != null)
                {
                    PasswordsGrid.ItemsSource = null;
                    PasswordsGrid.ItemsSource = itemsSource;
                }

                window.ShowDialog();
            }
        }

        private void ToggleConfirmPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            isConfirmPasswordVisible = !isConfirmPasswordVisible;
            if (isConfirmPasswordVisible)
            {
                confirmPasswordTextBox.Text = ConfirmMasterPasswordBox.Password;
                ConfirmMasterPasswordBox.Visibility = Visibility.Collapsed;
                confirmPasswordTextBox.Visibility = Visibility.Visible;
                ToggleConfirmPasswordButton.Content = ResourceManagerHelper.Instance.Eye;
            }
            else
            {
                ConfirmMasterPasswordBox.Password = confirmPasswordTextBox.Text;
                confirmPasswordTextBox.Visibility = Visibility.Collapsed;
                ConfirmMasterPasswordBox.Visibility = Visibility.Visible;
                ToggleConfirmPasswordButton.Content = ResourceManagerHelper.Instance.Eye;
            }
        }

        private void ToggleMasterPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            isMasterPasswordVisible = !isMasterPasswordVisible;
            if (isMasterPasswordVisible)
            {
                masterPasswordTextBox.Text = MasterPasswordBox.Password;
                MasterPasswordBox.Visibility = Visibility.Collapsed;
                masterPasswordTextBox.Visibility = Visibility.Visible;
                ToggleMasterPasswordButton.Content = ResourceManagerHelper.Instance.Eye;
            }
            else
            {
                MasterPasswordBox.Password = masterPasswordTextBox.Text;
                masterPasswordTextBox.Visibility = Visibility.Collapsed;
                MasterPasswordBox.Visibility = Visibility.Visible;
                ToggleMasterPasswordButton.Content = ResourceManagerHelper.Instance.Eye;
            }
        }

        private void ToggleNewPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            isNewPasswordVisible = !isNewPasswordVisible;
            if (isNewPasswordVisible)
            {
                newPasswordTextBox.Text = NewMasterPasswordBox.Password;
                NewMasterPasswordBox.Visibility = Visibility.Collapsed;
                newPasswordTextBox.Visibility = Visibility.Visible;
                ToggleNewPasswordButton.Content = ResourceManagerHelper.Instance.Eye;
            }
            else
            {
                NewMasterPasswordBox.Password = newPasswordTextBox.Text;
                newPasswordTextBox.Visibility = Visibility.Collapsed;
                NewMasterPasswordBox.Visibility = Visibility.Visible;
                ToggleNewPasswordButton.Content = ResourceManagerHelper.Instance.Eye;
            }
        }

        private void UpdatePassword(int id, string title, string category)
        {
            using var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            conn.Open();
            string sql = "UPDATE Passwords SET Title = @title, Category = @category WHERE Id = @id";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@category", category);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
    }
}