import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

const vkProxy = {
  '/vkplay-api': {
    target: 'https://api.live.vkvideo.ru',
    changeOrigin: true,
    rewrite: (path) => path.replace(/^\/vkplay-api/, '/v1'),
  },
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: { proxy: vkProxy },
  preview: { proxy: vkProxy },
})