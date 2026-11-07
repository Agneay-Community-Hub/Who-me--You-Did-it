using System.Collections.Generic;
using UnityEngine;

public class Suspect
{
    public string name, role, dossier, pitch, reaction, verdict, features;
    public int absurdity; // 1 (plausible) .. 5 (completely unhinged)
    public Color color;
}

public class Evidence
{
    public string title, text, aside;
    /// suspect index -> why this evidence rules them out
    public Dictionary<int, string> rules = new Dictionary<int, string>();
}

public class CaseData
{
    public string title, location, you, youFeatures, crime, truth, epilogue, busted;
    public Color youColor;
    public string[] intro;
    public Suspect[] suspects;
    public Evidence[] evidence;
}

public static class Cases
{
    public static readonly Color InspectorColor = new Color(0.85f, 0.78f, 0.7f);
    public const string InspectorFeatures = "fedora";

    static Color C(float r, float g, float b) => new Color(r, g, b);

    public static readonly CaseData[] All =
    {
        // ------------------------------------------------------------------ CASE 1
        new CaseData
        {
            title = "The Midnight Cake",
            location = "Bramblewood Manor, 11:58 PM",
            you = "Reginald (you)", youColor = C(0.9f, 0.75f, 0.62f), youFeatures = "mustache,frosting",
            crime = "The Duchess's 100th birthday cake - three tiers of strawberry buttercream - has been devoured. Only the plate remains. And a napkin.",
            truth = "You ate it. All of it. At 11:55 PM, in the dark, floating across the flour-dusted floor in your socks, dabbing your lips with a napkin like a gentleman. You never left the room. There is still frosting on your moustache.\n\nNow someone else has to take the fall.",
            intro = new[]
            {
                "Inspector Prudence Gloom. Bureau of Baked-Goods Crime.",
                "Someone has eaten the Duchess's 100th birthday cake. All three tiers.",
                "You're the nephew. You were 'just passing'. So tell me - who did it?",
                "(Click a suspect's card to read their file. Accuse someone - anyone - but never contradict the evidence.)",
            },
            epilogue = "As Great-Grandpa Bramble's portrait is carried off to the county jail, you quietly lick the last of the frosting from your moustache. Happy birthday, Duchess.",
            busted = "Inspector Gloom wipes a smear of pink frosting from your moustache and tastes it. \"Strawberry buttercream,\" she says. \"You're nicked.\"",
            suspects = new[]
            {
                new Suspect { name = "Hollis", role = "The Butler", absurdity = 1, color = C(0.80f, 0.70f, 0.60f), features = "bowtie",
                    dossier = "Forty years of loyal service. Despises sugar. Spends every night from 11 PM until midnight polishing the silver in front of three bored footmen.",
                    pitch = "It was the butler. It is ALWAYS the butler. Look at those smug, spotless gloves.",
                    reaction = "The butler. How original. ...But I suppose it's possible.",
                    verdict = "Hollis is led away, gloves still spotless. \"Very good, sir,\" he says, with a look that will haunt you forever." },
                new Suspect { name = "Mrs. Dumpling", role = "The Cook", absurdity = 1, color = C(0.95f, 0.72f, 0.62f), features = "chef",
                    dossier = "Baked the cake herself. Goes to bed at 10 PM sharp. Her snoring has been officially registered by the seismograph in the next village.",
                    pitch = "The cook! She knew that cake's every weakness. She MADE its weaknesses.",
                    reaction = "Motive: unclear. Opportunity: she had the recipe. Hmm.",
                    verdict = "Mrs. Dumpling is taken away, muttering about buttercream ratios. Breakfast will never be the same." },
                new Suspect { name = "Colonel Ashby", role = "Retired Colonel", absurdity = 2, color = C(0.86f, 0.58f, 0.50f), features = "mustache,monocle",
                    dossier = "Formerly of the 3rd Light Snackery. Wears hobnail boots at all times, including in the bath. Was up at midnight 'guarding the perimeter'.",
                    pitch = "The Colonel. A military man. Trained to consume rations under extreme pressure.",
                    reaction = "The Colonel has the appetite, I'll grant you.",
                    verdict = "The Colonel salutes as they march him off. \"Tell the regiment I went out like a soldier. A slightly sticky soldier.\"" },
                new Suspect { name = "Lady Fenwick", role = "The Duchess's Rival", absurdity = 2, color = C(0.85f, 0.78f, 0.92f), features = "bow",
                    dossier = "Has hated the Duchess since a croquet incident in 1961. Wears stiletto heels sharp enough to core an apple. Stayed the night purely out of spite.",
                    pitch = "Lady Fenwick! Sixty years of rivalry, and she finally struck where it hurts: dessert.",
                    reaction = "Sixty years of spite is a lot of motive. Go on.",
                    verdict = "Lady Fenwick is arrested and has never been happier. \"At last, the Duchess will know I ruined her birthday.\"" },
                new Suspect { name = "Sir Biscuit", role = "The Corgi", absurdity = 3, color = C(0.93f, 0.65f, 0.30f), features = "ears",
                    dossier = "A very good boy, allegedly. Leaves muddy paw prints on absolutely everything - including, somehow, the ceiling. Was off his lead at midnight.",
                    pitch = "The dog. Look into his eyes. Those are the eyes of a cake criminal.",
                    reaction = "You're blaming the DOG? ...He does look shifty.",
                    verdict = "Sir Biscuit is sentenced to six months without belly rubs. He takes it like a very good boy." },
                new Suspect { name = "Vac-U-Tron 3000", role = "Robot Vacuum", absurdity = 4, color = C(0.62f, 0.67f, 0.75f), features = "antenna",
                    dossier = "Autonomous cleaning unit. Hoovers up anything in its path. Leaves no footprints - it vacuums them. No manners, no napkins, no concept of dignity.",
                    pitch = "The robot vacuum. It roams at night. It consumes. It was only a matter of time.",
                    reaction = "A household appliance. Bold. ...Your honour, I'm going to allow it.",
                    verdict = "Vac-U-Tron 3000 is taken into custody, still beeping. It has requested a lawyer. Its lawyer is a toaster." },
                new Suspect { name = "The Duchess", role = "The Birthday Girl", absurdity = 4, color = C(0.96f, 0.86f, 0.82f), features = "crown",
                    dossier = "Turning 100. Legendary sweet tooth (her last one). Is carried everywhere in a sedan chair and never touches the floor. Leaves for her telenovela at 12:05 every night, without fail.",
                    pitch = "The Duchess ate her own cake! Insurance fraud. Classic.",
                    reaction = "You're accusing the VICTIM? Of eating her OWN birthday cake?",
                    verdict = "The Duchess is arrested on her own birthday. She calls it \"the most exciting thing to happen since the war\" and refuses to say which war." },
                new Suspect { name = "Great-Grandpa Bramble", role = "Oil Portrait (1802)", absurdity = 5, color = C(0.74f, 0.62f, 0.46f), features = "frame,mustache",
                    dossier = "Has hung in the dining room for 224 years and has never once left it. Impeccable table manners, for a painting. His painted eyes follow you around the room. Fine painted moustache.",
                    pitch = "It was the painting. His eyes follow you. Do you know what else follows you? Guilt.",
                    reaction = "...A painting. You are accusing a PAINTING. Of eating a cake.",
                    verdict = "Great-Grandpa Bramble is sentenced to hang. Which, to be fair, is what he was already doing." },
            },
            evidence = new[]
            {
                new Evidence { title = "The Timeline", text = "The cake was devoured between 11:50 PM and midnight.",
                    rules = { { 0, "Hollis was polishing silver in front of three footmen until the stroke of midnight. Boring witnesses, but reliable." },
                              { 1, "Mrs. Dumpling was asleep since ten. The seismograph in the next village confirms it." } } },
                new Evidence { title = "The Flour", text = "The dining room floor was dusted with flour. Not one footprint or paw print was found in it.",
                    rules = { { 2, "The Colonel wears hobnail boots. He leaves footprints in solid granite." },
                              { 3, "Lady Fenwick's stilettos would have punched holes in the floorboards, never mind the flour." },
                              { 4, "Sir Biscuit leaves paw prints on the CEILING. He'd hardly spare the flour." } },
                    aside = "Whoever did this didn't touch the floor. Curious." },
                new Evidence { title = "The Napkin", text = "A linen napkin was found, neatly folded. The culprit dabbed their lips. With dignity.",
                    rules = { { 5, "Vac-U-Tron has no lips, no napkin and no dignity. It once ate a sock and blamed the cat." } } },
                new Evidence { title = "The Door", text = "A footman watched the only door from 11:50 PM until dawn. Nobody left the dining room.",
                    rules = { { 6, "The Duchess left at 12:05 for her telenovela. She hasn't missed an episode since 1974." } },
                    aside = "So the culprit is still in this room. Somewhere." },
                new Evidence { title = "The Frosting", text = "A smear of pink frosting was found on the culprit's moustache.",
                    aside = "Which narrows it down considerably. Moustaches only, please." },
            },
        },

        // ------------------------------------------------------------------ CASE 2
        new CaseData
        {
            title = "The Moonstone Swap",
            location = "Grimsby Museum of Things, 2:13 AM",
            you = "Dez (you)", youColor = C(0.80f, 0.64f, 0.50f), youFeatures = "cap",
            crime = "The priceless Moonstone of Varnia has been stolen from the Gem Room and replaced with a gumball. A blue one. The museum is furious. The gumball is fine.",
            truth = "You're the night guard. You were bored. At 2:13 AM you stood perfectly still for an hour to slip past the laser grid, said nothing, leaned on the old marble statue (you're covered in dust), and flicked a gumball into the case from across the room. Great shot, honestly.\n\nThe Moonstone is in your sock.",
            intro = new[]
            {
                "Inspector Gloom again. Bureau of Shiny-Things Crime, this time. I transferred.",
                "The Moonstone of Varnia was swapped for a gumball during your shift, guard.",
                "Funny, that. So - who did it?",
            },
            epilogue = "The Discus Thrower is wheeled into court on a dolly. The jury agrees he looks guilty - he hasn't blinked once. You pat the Moonstone in your sock and start your next shift.",
            busted = "Inspector Gloom brushes the marble dust off your uniform and pulls the Moonstone out of your sock. \"A gumball, Dez. A GUMBALL.\"",
            suspects = new[]
            {
                new Suspect { name = "Dr. Pembers", role = "The Curator", absurdity = 1, color = C(0.85f, 0.75f, 0.65f), features = "monocle",
                    dossier = "Runs the museum. Was giving a livestreamed lecture, 'Rocks: Why?', from 1 AM to 3 AM. Eleven people watched. Nine of them were his mum.",
                    pitch = "The curator! An inside job. He had the keys, the motive, and the monocle.",
                    reaction = "The curator. Plausible. Boringly plausible.",
                    verdict = "Dr. Pembers is arrested mid-lecture. His mum gives the arrest five stars." },
                new Suspect { name = "The Velvet Glove", role = "Infamous Cat Burglar", absurdity = 1, color = C(0.72f, 0.52f, 0.78f), features = "mask",
                    dossier = "Has stolen four crowns, a pier and a Tuesday. Always leaves a dramatic monologue at the scene. 'It's my brand, darling.' Was in town that night.",
                    pitch = "The Velvet Glove! The world's greatest thief. Obviously.",
                    reaction = "A famous jewel thief, for a jewel theft. ...That's almost too sensible for you.",
                    verdict = "The Velvet Glove is caught at last and delivers a forty-minute monologue at sentencing. The judge adds ten years for length." },
                new Suspect { name = "Gus", role = "The Janitor", absurdity = 2, color = C(0.80f, 0.68f, 0.55f), features = "mustache,cap",
                    dossier = "Mops the Gem Room every night. Clocks out at 2:00 AM on the dot. Union rules. Has never worked one second of overtime in his life.",
                    pitch = "The janitor. Nobody suspects the janitor. Which is exactly why I do.",
                    reaction = "The janitor? He'd have had access...",
                    verdict = "Gus is arrested, and immediately files a grievance with his union about being arrested outside working hours." },
                new Suspect { name = "The Kowalskis", role = "Tourists (all six)", absurdity = 3, color = C(0.95f, 0.80f, 0.50f), features = "glasses",
                    dossier = "Got lost on the tour and slept in the gift shop. Have not stopped talking since 1997, including in their sleep. Especially in their sleep.",
                    pitch = "The tourist family. Six of them. That's six times the suspicion.",
                    reaction = "An entire family of tourists. As one criminal unit. ...Hm.",
                    verdict = "All six Kowalskis are arrested. They describe the cell as \"cosy\" and leave it a lovely online review." },
                new Suspect { name = "Marcel", role = "Street Mime", absurdity = 3, color = C(0.97f, 0.97f, 0.97f), features = "beret",
                    dossier = "Sleeps in the museum lobby 'for the ambience'. Utterly silent. Can stand perfectly still for hours inside an invisible box. Face caked in white greasepaint.",
                    pitch = "The mime. Silent. Still. White-faced. He's practically confessing.",
                    reaction = "A mime. Silent, motionless... Hm. Annoyingly plausible.",
                    verdict = "Marcel is led away, pretending to be trapped in an invisible police car. Nobody can get him out." },
                new Suspect { name = "A Pigeon", role = "Pigeon", absurdity = 4, color = C(0.60f, 0.63f, 0.72f), features = "beak",
                    dossier = "Got in through the skylight. Has not said one word, because: pigeon. Physically incapable of holding still for longer than a second.",
                    pitch = "A pigeon. They love shiny things. Look it up. Don't look it up.",
                    reaction = "That's magpies. ...But fine. The pigeon is a person of interest.",
                    verdict = "The pigeon is sentenced to community service, which it performs by sitting on statues. As usual." },
                new Suspect { name = "Mr. Normal Human", role = "3 Raccoons in a Trench Coat", absurdity = 4, color = C(0.55f, 0.55f, 0.58f), features = "ears,mask",
                    dossier = "Signed the guest book as 'Mr. Normal Human, Definitely Tall'. Mostly silent. Cannot hold still - they keep arguing about whose turn it is to be the head.",
                    pitch = "That 'gentleman' is clearly three raccoons. Raccoons steal. I rest my case.",
                    reaction = "...He IS very wobbly. Go on.",
                    verdict = "Mr. Normal Human is arrested. At the station he splits into three and escapes through a vent. Nobody files the paperwork." },
                new Suspect { name = "The Discus Thrower", role = "Marble Statue", absurdity = 5, color = C(0.88f, 0.88f, 0.85f), features = "laurel",
                    dossier = "Ancient Greek marble. Stands in the Gem Room. Has not moved, blinked or spoken in 2,400 years. Has a very strong throwing arm. Theoretically.",
                    pitch = "The statue did it. He's been standing there for 2,400 years waiting for this moment.",
                    reaction = "A STATUE. A literal statue. ...Write it down, I suppose.",
                    verdict = "The Discus Thrower gets ten years. He'll serve it standing still, which honestly suits him." },
            },
            evidence = new[]
            {
                new Evidence { title = "The Alarm", text = "The Gem Room alarm was tripped at exactly 2:13 AM.",
                    rules = { { 0, "Dr. Pembers was mid-lecture at 2:13. On camera. Explaining why rocks." },
                              { 2, "Gus clocked out at 2:00 sharp. He'd sooner die than be at work at 2:13." } } },
                new Evidence { title = "The Microphones", text = "The Gem Room microphones recorded total silence. Not one single word.",
                    rules = { { 1, "The Velvet Glove ALWAYS monologues. We'd have forty minutes of 'You fools!' on tape." },
                              { 3, "The Kowalskis talk in their sleep. The microphones would have melted." } } },
                new Evidence { title = "The Laser Grid", text = "Laser logs show the culprit stood perfectly still for a full hour before the theft.",
                    rules = { { 5, "A pigeon. Perfectly still. For an hour. Have you ever MET a pigeon?" },
                              { 6, "Those raccoons can't stay still for ten seconds. They keep swapping who's on top." } } },
                new Evidence { title = "The Dust", text = "The display case is covered in fine white dust. The lab says: not chalk, not greasepaint. Marble.",
                    rules = { { 4, "Marcel's face is greasepaint, not marble. The lab was very clear about this. And slightly scared of him." } } },
                new Evidence { title = "The Gumball", text = "The gumball was thrown into the case from across the room. Whoever did it has a cracking throwing arm.",
                    aside = "Remarkable arm. Olympic, almost. Classical, even." },
            },
        },

        // ------------------------------------------------------------------ CASE 3
        new CaseData
        {
            title = "Lights Out at Puffin Point",
            location = "Puffin Point Lighthouse, 12:04 AM",
            you = "Morag (you)", youColor = C(0.82f, 0.70f, 0.60f), youFeatures = "mustache,cap",
            crime = "The lighthouse went dark at midnight. The luxury cruise ship SS Pomposity is now stuck on a sandbar with 400 very rich, very loud, very damp millionaires.",
            truth = "You're the lighthouse keeper. You rowed back from the pub at midnight, stomped up all 212 steps in your size-11 boots, and switched the lamp off by hand so you could finally get some sleep. You're still in your trench coat. Your flashlight is in your pocket.\n\nAnd here comes Inspector Gloom.",
            intro = new[]
            {
                "Inspector Gloom. Bureau of Maritime Mischief. I keep getting transferred. I can't think why.",
                "I rowed over the moment I heard. Well - around midnight, ish. Doesn't matter.",
                "Somebody switched off your lighthouse, keeper. Who?",
            },
            epilogue = "Inspector Gloom reads herself her rights, cuffs her own wrists, and marches herself to the rowboat. \"Excellent work, Inspector,\" she tells herself. You switch the lamp back on, yawn, and finally get some sleep.",
            busted = "Inspector Gloom shines her enormous flashlight over your size-11 boots, your trench coat, and the rowboat key around your neck. \"Funny,\" she says. \"For a moment there, I almost suspected myself.\"",
            suspects = new[]
            {
                new Suspect { name = "Pip", role = "Assistant Keeper", absurdity = 1, color = C(0.90f, 0.75f, 0.65f), features = "bow",
                    dossier = "Has lived on the island for 30 years. Has never once set foot in a boat - she gets seasick in the bath. Wears dainty size-6 boots.",
                    pitch = "Pip. The assistant. Always wanted my job. Classic lighthouse politics.",
                    reaction = "Lighthouse politics. Is that a thing? ...It's a thing now.",
                    verdict = "Pip is arrested and taken to the mainland by boat. She is sick the entire way, which she considers the real punishment." },
                new Suspect { name = "Captain Brassbuckle", role = "Captain, SS Pomposity", absurdity = 1, color = C(0.92f, 0.78f, 0.68f), features = "beard,cap",
                    dossier = "Ran his own ship aground. Claims he was 'on the bridge all night, steering beautifully'. Forty furious passengers agree he was on the bridge. Navigates purely by vibes.",
                    pitch = "The captain sabotaged the light to cover up his own terrible driving.",
                    reaction = "Insurance fraud on the high seas? Plausible.",
                    verdict = "Captain Brassbuckle is arrested and steers the police van into a hedge. He calls it \"a beautiful parking job\"." },
                new Suspect { name = "The Seagull Gang", role = "Local Hooligans", absurdity = 3, color = C(0.96f, 0.96f, 0.96f), features = "beak,bandana",
                    dossier = "Six gulls with a grudge and a taste for chips. Tiny webbed feet. Have broken into the lamp room before to nest in the bulb.",
                    pitch = "The seagulls. They've been planning this for years. Look at their little bandanas.",
                    reaction = "The seagulls are organised now? ...They did take my chips earlier.",
                    verdict = "The Seagull Gang is sentenced to life on a remote island, which is where they already live. They celebrate with chips." },
                new Suspect { name = "The Fog", role = "Weather", absurdity = 3, color = C(0.76f, 0.79f, 0.84f), features = "wisps",
                    dossier = "Rolled in at 11:45 PM. Gets absolutely everywhere. Has no feet, no hands and very questionable intentions.",
                    pitch = "The fog did it. It's been hanging around all night looking moody.",
                    reaction = "Arresting the weather. That'll look great in my report.",
                    verdict = "The Fog is arrested but drifts through the bars by morning. The police issue a strongly worded weather warning." },
                new Suspect { name = "Gerald", role = "A Very Large Crab", absurdity = 4, color = C(0.90f, 0.36f, 0.30f), features = "claws",
                    dossier = "Lives under the jetty. Size: alarming. Has eight feet and owns no boots, although he once tried on four pairs at once. Strong pincers. Hates the light.",
                    pitch = "Gerald the crab. He hates the light. He has pincers. Do the maths.",
                    reaction = "The crab. You want me to arrest a crab. ...He does have form.",
                    verdict = "Gerald is sentenced to ten years. He serves them sideways." },
                new Suspect { name = "A Mermaid", role = "Folklore (Allegedly)", absurdity = 4, color = C(0.55f, 0.85f, 0.80f), features = "bow",
                    dossier = "Several sailors swear they saw her. Lovely singing voice. A tail where her legs should be. Arrived by sea, obviously.",
                    pitch = "A mermaid! She lured that ship onto the sandbar. It's in all the old songs.",
                    reaction = "A mythical creature. Splendid. I'll need a bigger notebook.",
                    verdict = "The mermaid is arrested and sings her way out of prison. The guards are now in a folk band." },
                new Suspect { name = "The Moon", role = "Celestial Body", absurdity = 5, color = C(0.95f, 0.93f, 0.75f), features = "craters",
                    dossier = "Controls the tides. 384,400 km away. Has been suspiciously bright lately, as if it doesn't need a lighthouse. No hands.",
                    pitch = "The Moon. Jealous of the lighthouse. Wanted to be the only light in the sky.",
                    reaction = "The Moon. You're accusing. The Moon.",
                    verdict = "The Moon is found guilty in absentia. It continues to be smug about it every night." },
                new Suspect { name = "Inspector Gloom", role = "Lead Investigator", absurdity = 5, color = InspectorColor, features = "fedora",
                    dossier = "Leading this investigation. Rowed to the island 'around midnight, ish'. Size-11 boots. Owns an enormous flashlight. Was 'admiring the view from the lamp room'. Insists none of this is relevant.",
                    pitch = "It was you, Inspector. You did it.",
                    reaction = "ME?! I... that's... absurd. Let's look at the evidence. Calmly. Very calmly.",
                    verdict = "Inspector Gloom arrests herself, then interrogates herself for six hours. She finally cracks and confesses to herself." },
            },
            evidence = new[]
            {
                new Evidence { title = "The Switch", text = "The lamp was switched off by hand, from inside the lamp room at the top of the tower.",
                    rules = { { 1, "The captain was on the bridge of the Pomposity, steering into a sandbar with total confidence. Forty witnesses. All furious." },
                              { 6, "The Moon has no hands and is 384,400 km away. Even for you, that's a stretch." } } },
                new Evidence { title = "The Footprints", text = "Wet footprints lead up all 212 stairs. Big, flat, two-footed, human-shaped footprints.",
                    rules = { { 2, "Seagull feet leave tiny arrow prints. These are human-shaped." },
                              { 3, "The Fog has no feet. At best, it loomed up the stairs." },
                              { 5, "Mermaids have tails. That's... sort of the whole thing about mermaids." } } },
                new Evidence { title = "The Boots", text = "The prints are from size-11 boots. Muddy ones.",
                    rules = { { 0, "Pip wears a dainty size 6. She'd need five extra inches of foot." },
                              { 4, "Gerald owns no boots. The four-pairs incident was a one-off and we don't talk about it." } },
                    aside = "Size eleven. Lots of people have size eleven. Completely normal size." },
                new Evidence { title = "The Rowboat", text = "A rowboat, still wet, is tied to the jetty. The culprit rowed here around midnight.",
                    aside = "...Lots of people own rowboats. It's an island." },
                new Evidence { title = "The Flashlight", text = "Nobody climbs a lighthouse in the dark without a flashlight. A big one.",
                    aside = "My flashlight is a perfectly normal size for a flashlight. Moving on." },
                new Evidence { title = "The Coat", text = "A witness saw the culprit wearing a trench coat. Like the one the Inspector is wearing right now.",
                    aside = "...Huh." },
            },
        },
    };
}
