import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import { env } from 'node:process';

const target = env.ASPNETCORE_URLS?.split(';')[0] || 'http://localhost:5272';

export default defineConfig({
    plugins: [plugin()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        proxy: {
            '^/api': { target },
            '^/hubs': { target, ws: true }
        },
        port: Number.parseInt(env.DEV_SERVER_PORT || '57290', 10),
        strictPort: true
    }
});
