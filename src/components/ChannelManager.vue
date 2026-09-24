<script setup>
import { ref } from 'vue'
import FileImportButton from './FileImportButton.vue'
import { platforms } from '../platforms'

defineProps({
  channels: { type: Array, required: true },
})

const emit = defineEmits(['add', 'import', 'export'])

const draft = ref('')
const platform = ref(platforms[0]?.id)

function add() {
  const value = draft.value
  draft.value = ''
  if (value.trim()) emit('add', value, platform.value)
}

function onImport(text) {
  emit('import', text)
}
</script>

<template>
  <section class="panel">
    <h2>Каналы</h2>

    <div class="add-row">
      <select v-model="platform">
        <option v-for="p in platforms" :key="p.id" :value="p.id">{{ p.name }}</option>
      </select>
      <input v-model="draft" type="text" placeholder="Имя канала" @keyup.enter="add" />
      <button type="button" class="btn btn-primary" :disabled="!draft.trim()" @click="add">
        Добавить
      </button>
    </div>

    <div class="btn-row">
      <FileImportButton label="Импорт channels.json" accept=".json,application/json" @import="onImport" />
      <button type="button" class="btn btn-secondary" :disabled="!channels.length" @click="emit('export')">
        Экспорт channels.json
      </button>
    </div>
  </section>
</template>