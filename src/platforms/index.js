import { twitch } from './twitch'
import { kick } from './kick'

export const platforms = [twitch, kick]

export function getPlatform(id) {
  return platforms.find((p) => p.id === id) || platforms[0]
}