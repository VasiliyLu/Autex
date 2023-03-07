FROM node:lts as build
WORKDIR /src
COPY . .
RUN npm install --production=false
RUN npm run build

FROM nginx as final
COPY nginx/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist /usr/share/nginx/html
EXPOSE 80