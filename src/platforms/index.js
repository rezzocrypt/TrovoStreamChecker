import { twitch } from './twitch'
import { kick } from './kick'
import { vkplay } from './vkplay'

export const platforms = [twitch, kick, vkplay]

export function getPlatform(id) {
  return platforms.find((p) => p.id === id) || platforms[0]
}