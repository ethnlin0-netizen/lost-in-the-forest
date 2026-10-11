"""Original synthesized instrumental score. No recordings or third-party music used."""
from pathlib import Path
import json
import wave
import numpy as np

SR = 44100
OUT = Path(__file__).resolve().parents[1] / 'Assets/Audio/Music/Originals'
OUT.mkdir(parents=True, exist_ok=True)
RNG = np.random.default_rng(510310)

def tone(note, duration, voice):
    t = np.arange(max(1, int(duration * SR)), dtype=np.float64) / SR
    f = 440 * 2 ** ((note - 69) / 12)
    phase = 2 * np.pi * f * t
    attack = .006 if voice in ('piano', 'pluck', 'bell') else .18
    release = .12 if voice in ('piano', 'pluck') else .35
    env = np.minimum(1, t / attack) * np.minimum(1, (duration - t) / release)
    if voice == 'piano':
        x = sum(np.sin(phase * k * (1 + .0001*k*k)) * np.exp(-t*(.8 + .38*k)) / k**1.6 for k in range(1, 9))
    elif voice == 'bell':
        x = sum(np.sin(phase * r) * np.exp(-t*d) * a for r,a,d in [(1,1,.6),(2.001,.38,.9),(2.76,.16,1.8),(4.05,.07,2.3)])
    elif voice == 'pluck':
        x = sum(np.sin(phase*k) * np.exp(-t*(2.7+k*.6))/k**1.3 for k in range(1, 9))
    elif voice == 'bass':
        x = np.sin(phase) + .24*np.sin(phase*2) + .07*np.sin(phase*3)
        env *= np.exp(-t * .5)
    else:
        x = np.zeros_like(t)
        for k in range(1, 10):
            if f*k < 17000:
                x += (np.sin(phase*k + .011*k*np.sin(t*2*np.pi*4.7)) + .35*np.sin(phase*k*1.0015)) / k**1.8
        env *= .85 + .15*np.sin(t*2*np.pi*.8)
    return (x * env / 1.6).astype(np.float32)

def percussion(kind):
    duration = {'kick':.7, 'snare':.28, 'hat':.12, 'boom':1.8}[kind]
    t = np.arange(int(duration*SR)) / SR
    noise = RNG.standard_normal(t.size)
    high = noise - np.roll(noise, 1)
    if kind in ('kick', 'boom'):
        pitch = (42 if kind == 'boom' else 48) + (50 if kind == 'boom' else 100)*np.exp(-t*22)
        x = np.sin(2*np.pi*np.cumsum(pitch)/SR)*np.exp(-t*(2.8 if kind=='boom' else 7))
        x += .025*high*np.exp(-t*40)
    elif kind == 'snare':
        x = .32*high*np.exp(-t*17) + .35*np.sin(2*np.pi*165*t)*np.exp(-t*22)
    else:
        x = .16*high*np.exp(-t*45)
    x *= np.minimum(1,t/.001)*np.minimum(1,(duration-t)/.01)
    return x.astype(np.float32)

def compose(name, bpm, bars, chords, mood):
    beat = 60 / bpm
    length = int(round(bars*4*beat*SR))
    audio = np.zeros((length, 2), dtype=np.float32)
    def add(samples, when, gain, pan=0):
        start = int(round(when*beat*SR)) % length
        stereo = samples[:,None]*gain*np.array([np.cos((pan+1)*np.pi/4),np.sin((pan+1)*np.pi/4)],np.float32)
        # Wrap every note release across the loop, including the final bar's decay.
        at=0
        while at < len(samples):
            n=min(length-start,len(samples)-at)
            audio[start:start+n] += stereo[at:at+n]
            at+=n;start=0
    def note(p,when,beats,voice,gain,pan=0):
        add(tone(p,beats*beat,voice),when,gain,pan)
    melody = {
        'ash': [[74,77,76,72],[69,72,74,77],[70,74,77,76],[69,73,76,73]],
        'root': [[62,65,63,62],[58,62,65,63],[55,58,62,60],[57,61,64,63]],
        'ice': [[86,81,82,79],[82,77,79,74],[81,77,79,75],[85,81,79,73]],
        'boss': [[62,63,69,65],[58,62,65,63],[55,58,62,60],[57,61,64,63]],
    }[mood]
    for bar in range(bars):
        phrase=(bar//4)%4
        chord=chords[bar%4]
        when=bar*4
        for i,p in enumerate(chord):
            note(p,when+i*.012,4.35,'strings',.14 if mood!='boss' else .19,(-.6,0,.6,.2)[i%4])
        bass=chord[0]-12
        for offset in ([0,2] if mood in ('ash','ice') else [0,1.5,2,3]):
            note(bass,when+offset,1.6,'bass',.15 if mood!='boss' else .24,-.05)
        pattern=[0,1,2,1,0,2,1,3]
        arp_voice='bell' if mood=='ice' else 'pluck'
        for step,degree in enumerate(pattern):
            # A shared rising/falling figure links the increasingly corrupted areas.
            if mood=='ash' and bar%4==3 and step>3:continue
            note(chord[degree%len(chord)]+12,when+step*.5,1.25,arp_voice,.085 if mood!='boss' else .13,np.sin(step)*.55)
        lead_voice='bell' if mood=='ice' else 'strings' if mood=='boss' else 'piano'
        for i,p in enumerate(melody[bar%4]):
            if mood in ('ash','ice') and i%2==1 and phrase==0:continue
            octave=12 if mood=='boss' and phrase==2 else 0
            note(p+octave,when+i+.12,1.8 if mood!='boss' else .85,lead_voice,.21 if mood=='ash' else .18,.12)
        if mood=='ash':
            if phrase>=1:add(percussion('boom'),when,.055,0)
        elif mood=='root':
            for offset in [0,2.5]:add(percussion('boom'),when+offset,.16,0)
            if bar%2:add(percussion('hat'),when+3,.045,.4)
        elif mood=='ice':
            if bar%2==0:add(percussion('boom'),when,.07,0)
            note(38,when,4.4,'strings',.07,-.3)
            if phrase>=2:note(87,when+3.5,2.4,'bell',.055,-.65)
        else:
            for offset in [0,1.5,2,3.5]:add(percussion('kick'),when+offset,.42,0)
            for offset in [1,3]:add(percussion('snare'),when+offset,.22,.1)
            for offset in np.arange(0,4,.5):add(percussion('hat'),when+offset,.12 if offset%1 else .16,.35)
            if bar%4==0:add(percussion('boom'),when,.3,-.2)
            if bar%8==7:
                for offset in [3,3.25,3.5,3.75]:add(percussion('snare'),when+offset,.11,-.15)
            for step in range(8):note(chord[(step//2)%3],when+step*.5,.32,'strings',.12,-.3)
    dry=audio.copy()
    # Circular stereo room, so the ambience also continues across the loop boundary.
    for delay,gain in [(.083,.15),(.139,.12),(.229,.10),(.347,.08),(.503,.065),(.719,.05),(.997,.035), (1.331,.02)]:
        audio += np.roll(dry[:,::-1] if int(delay*1000)%2 else dry,int(delay*SR),axis=0)*gain
    audio -= audio.mean(axis=0)
    target_rms=10**((-19 if mood!='boss' else -17)/20)
    audio *= min(target_rms/np.sqrt(np.mean(audio**2)),.79/np.max(np.abs(audio)))
    pcm=np.round(audio*32767).astype('<i2')
    path=OUT/(name+'.wav')
    with wave.open(str(path),'wb') as out:
        out.setnchannels(2);out.setsampwidth(2);out.setframerate(SR);out.writeframes(pcm.tobytes())
    boundary=float(np.max(np.abs(audio[0]-audio[-1])))
    assert np.isfinite(audio).all() and np.max(np.abs(audio))<.8
    assert boundary < .035, f'Loop discontinuity: {boundary}'
    report={'file':path.name,'bpm':bpm,'bars':bars,'seconds':length/SR,'peak_dbfs':float(20*np.log10(np.max(np.abs(audio)))),'rms_dbfs':float(20*np.log10(np.sqrt(np.mean(audio**2)))),'loop_boundary_step':boundary}
    print(json.dumps(report),flush=True)
    return report

tracks=[
    ('Level03_Ashpetal_Embers',76,16,[[50,57,65,69],[46,53,62,65],[43,50,58,62],[45,52,61,64]],'ash'),
    ('Level04_Veinroot_Pulse',84,16,[[50,57,63,65],[46,53,62,65],[43,50,58,62],[45,52,61,64]],'root'),
    ('Level05_Wraithglass_Echoes',88,16,[[50,57,65,69],[46,53,62,65],[43,50,58,63],[45,52,61,64]],'ice'),
    ('Boss_Dreadcrown_Amalgam',132,32,[[38,45,51,53],[34,41,50,53],[31,38,46,50],[33,40,49,52]],'boss'),
]
if __name__ == '__main__':
    reports=[compose(*track) for track in tracks]
    (OUT/'Verification.json').write_text(json.dumps(reports,indent=2)+'\n')
