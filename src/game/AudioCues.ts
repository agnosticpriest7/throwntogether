export type Cue = "pickup" | "throw" | "incoming" | "catch" | "miss" | "process" | "complete" | "purchase" | "order" | "orderComplete" | "expire" | "serviceStart" | "serviceEnd";

const CUES: Record<Cue, [number, number, OscillatorType]> = {
  pickup: [520, 0.07, "sine"],
  throw: [220, 0.11, "sawtooth"],
  incoming: [740, 0.16, "sine"],
  catch: [620, 0.13, "triangle"],
  miss: [105, 0.22, "square"],
  process: [350, 0.12, "square"],
  complete: [880, 0.2, "sine"],
  purchase: [660, 0.08, "triangle"],
  order: [740, 0.18, "triangle"],
  orderComplete: [980, 0.22, "sine"],
  expire: [150, 0.24, "sawtooth"],
  serviceStart: [520, 0.28, "triangle"],
  serviceEnd: [260, 0.32, "sine"],
};

export class AudioCues {
  private context?: AudioContext;

  play(cue: Cue): void {
    this.context ??= new AudioContext();
    if (this.context.state === "suspended") void this.context.resume();
    const [frequency, duration, type] = CUES[cue];
    const oscillator = this.context.createOscillator();
    const gain = this.context.createGain();
    oscillator.type = type;
    oscillator.frequency.setValueAtTime(frequency, this.context.currentTime);
    if (cue === "throw" || cue === "miss") oscillator.frequency.exponentialRampToValueAtTime(frequency * 0.55, this.context.currentTime + duration);
    gain.gain.setValueAtTime(0.06, this.context.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.001, this.context.currentTime + duration);
    oscillator.connect(gain).connect(this.context.destination);
    oscillator.start();
    oscillator.stop(this.context.currentTime + duration);
  }
}
