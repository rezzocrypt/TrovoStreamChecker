<script setup>
import { getPlatform } from '../platforms'
import SvgIcon from './SvgIcon.vue'

defineProps({
  rows: { type: Array, required: true },
})

defineEmits(['remove'])

function formatViewers(v) {
  if (v === null || v === undefined) return '—'
  return Number(v).toLocaleString('ru-RU')
}

function formatText(v) {
  return v ?? '—'
}

function parseKickTime(v) {
  return /^\d{4}-\d{2}-\d{2} \d{2}:\d{2}/.test(v) ? v.replace(' ', 'T') + 'Z' : v
}

function formatStart(v) {
  if (!v) return '—'
  const date = new Date(parseKickTime(v))
  if (Number.isNaN(date.getTime())) return '—'
  const time = date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
  if (date.toDateString() === new Date().toDateString()) return time
  const opts = { day: 'numeric', month: 'short' }
  if (date.getFullYear() !== new Date().getFullYear()) opts.year = 'numeric'
  return `${time}, ${date.toLocaleDateString('ru-RU', opts)}`
}

function initial(name) {
  return String(name).charAt(0).toUpperCase()
}

function platformById(id) {
  return getPlatform(id)
}
</script>

<template>
  <table>
    <thead>
      <tr>
        <th class="col-platform"></th>
        <th class="col-status">
          <SvgIcon name="broadcast" class="th-icon" />
        </th>
        <th class="col-channel">Стример</th>
        <th class="col-viewers">
          <SvgIcon name="users" class="th-icon" />
        </th>
        <th class="col-start">Начало</th>
        <th class="col-game">Игра</th>
        <th class="col-title">Название</th>
        <th class="col-remove"></th>
      </tr>
    </thead>
    <tbody>
      <tr v-if="!rows.length">
        <td colspan="7" class="empty">
          Каналов нет — добавьте их в списке выше или импортируйте channels.json.
        </td>
      </tr>
      <tr v-for="row in rows" :key="`${row.platform}:${row.name.toLowerCase()}`">
        <td class="col-platform" :title="platformById(row.platform)?.name">
          <span :class="`platform-icon platform-${row.platform}`"></span>
        </td>
        <td class="col-status">
          <span v-if="row.error" class="dot dot-error" title="Канал не найден"></span>
          <span v-else-if="row.online === null" class="dot dot-updating" title="обновляю…"></span>
          <span v-else-if="row.online" class="dot dot-live" title="Онлайн"></span>
          <span v-else class="dot dot-offline" title="Оффлайн"></span>
        </td>
        <td class="col-channel" :title="row.name">
          <img v-if="row.avatar" :src="row.avatar" class="avatar" />
          <span v-else class="avatar avatar-fallback">{{ initial(row.name) }}</span>
          <a
            :href="platformById(row.platform)?.channelUrl?.(row.name) || '#'"
            target="_blank"
            rel="noopener"
          >
            {{ row.name }}
          </a>
        </td>
        <td class="col-viewers" :title="row.viewers != null ? formatViewers(row.viewers) : null">{{ formatViewers(row.viewers) }}</td>
        <td class="col-start" :title="row.startedAt ? formatStart(row.startedAt) : null">{{ formatStart(row.startedAt) }}</td>
        <td class="col-game" :title="row.game ? formatText(row.game) : null">{{ formatText(row.game) }}</td>
        <td class="col-title" :title="row.title ? formatText(row.title) : null">{{ formatText(row.title) }}</td>
        <td class="col-remove">
          <button type="button" class="tag-remove" title="Убрать канал" @click="$emit('remove', row.platform, row.name)">
            <SvgIcon name="trash" />
          </button>
        </td>
      </tr>
    </tbody>
  </table>
</template>