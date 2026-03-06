'use client';

import * as React from 'react';
import Link from 'next/link';
import { Facebook, Instagram, Twitter, Youtube, Mail, Phone, MapPin } from 'lucide-react';
import { useSiteConfig } from '@/providers/site-config-provider';

const footerLinks = {
  marketplace: {
    title: 'Marketplace',
    links: [
      { href: '/vehiculos', label: 'Buscar Vehículos' },
      { href: '/vender', label: 'Vender tu Vehículo' },
      { href: '/dealers', label: 'Para Dealers' },
      { href: '/precios', label: 'Guía de Precios' },
      { href: '/comparar', label: 'Comparar Vehículos' },
      { href: '/herramientas', label: 'Herramientas' },
    ],
  },
  company: {
    title: 'Compañía',
    links: [
      { href: '/nosotros', label: 'Sobre Nosotros' },
      { href: '/contacto', label: 'Contacto' },
      { href: '/blog', label: 'Blog' },
      { href: '/prensa', label: 'Prensa' },
      { href: '/empleos', label: 'Empleos' },
    ],
  },
  legal: {
    title: 'Legal',
    links: [
      { href: '/terminos', label: 'Términos de Servicio' },
      { href: '/privacidad', label: 'Política de Privacidad' },
      { href: '/cookies', label: 'Política de Cookies' },
      { href: '/seguridad', label: 'Seguridad' },
    ],
  },
  support: {
    title: 'Soporte',
    links: [
      { href: '/ayuda', label: 'Centro de Ayuda' },
      { href: '/faq', label: 'Preguntas Frecuentes' },
      { href: '/guias', label: 'Guías de Compra' },
      { href: '/herramientas/calculadora-financiamiento', label: 'Calculadora Financiamiento' },
      { href: '/herramientas/calculadora-importacion', label: 'Calculadora Importación' },
      { href: '/reportar', label: 'Reportar Fraude' },
    ],
  },
};

export function Footer() {
  const config = useSiteConfig();
  const currentYear = new Date().getFullYear();

  const dynamicSocialLinks = [
    { href: config.socialFacebook, icon: Facebook, label: 'Facebook' },
    { href: config.socialInstagram, icon: Instagram, label: 'Instagram' },
    { href: config.socialTwitter, icon: Twitter, label: 'Twitter' },
    { href: config.socialYoutube, icon: Youtube, label: 'YouTube' },
  ];

  return (
    <footer className="border-border bg-muted/50 dark:bg-background border-t">
      {/* Main Footer */}
      <div className="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
        <div className="grid grid-cols-2 gap-8 md:grid-cols-4 lg:grid-cols-5">
          {/* Brand Column */}
          <div className="col-span-2 md:col-span-4 lg:col-span-1">
            <Link href="/" className="flex items-center gap-2">
              <div className="bg-primary flex h-10 w-10 items-center justify-center rounded-lg">
                <span className="text-primary-foreground text-xl font-bold">
                  {config.siteName.charAt(0)}
                </span>
              </div>
              <span className="text-foreground text-2xl font-bold">{config.siteName}</span>
            </Link>
            <p className="text-muted-foreground mt-4 text-sm">{config.siteDescription}</p>

            {/* Contact Info */}
            <div className="mt-6 space-y-2">
              <a
                href={`mailto:${config.contactEmail}`}
                className="text-muted-foreground hover:text-primary flex items-center gap-2 text-sm"
              >
                <Mail className="h-4 w-4" />
                {config.contactEmail}
              </a>
              <a
                href={config.phoneHref}
                className="text-muted-foreground hover:text-primary flex items-center gap-2 text-sm"
              >
                <Phone className="h-4 w-4" />
                {config.supportPhone}
              </a>
              <p className="text-muted-foreground flex items-center gap-2 text-sm">
                <MapPin className="h-4 w-4" />
                Santo Domingo, RD
              </p>
            </div>

            {/* Social Links */}
            <div className="mt-6 flex gap-3">
              {dynamicSocialLinks.map(social => (
                <a
                  key={social.label}
                  href={social.href}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="bg-muted text-muted-foreground hover:bg-primary hover:text-primary-foreground flex h-11 w-11 items-center justify-center rounded-lg transition-colors"
                  aria-label={social.label}
                >
                  <social.icon className="h-4 w-4" />
                </a>
              ))}
            </div>
          </div>

          {/* Links Columns */}
          {Object.entries(footerLinks).map(([key, section]) => (
            <div key={key}>
              <h3 className="text-foreground text-sm font-semibold">{section.title}</h3>
              <ul className="mt-4 space-y-0">
                {section.links.map(link => (
                  <li key={link.href}>
                    <Link
                      href={link.href}
                      className="text-muted-foreground hover:text-primary inline-block min-h-[44px] py-2 text-sm leading-[44px] transition-colors sm:min-h-0 sm:leading-normal"
                    >
                      {link.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </div>

      {/* Bottom Bar */}
      <div className="border-border bg-muted dark:bg-muted/50 border-t">
        <div className="mx-auto flex max-w-7xl flex-col items-center justify-between gap-4 px-4 py-6 sm:flex-row sm:px-6 lg:px-8">
          <p className="text-muted-foreground text-sm">
            © {currentYear} {config.siteName}. Todos los derechos reservados.
          </p>
          <div className="flex gap-2 sm:gap-6">
            <Link
              href="/terminos"
              className="text-muted-foreground hover:text-primary px-2 py-2 text-sm"
            >
              Términos
            </Link>
            <Link
              href="/privacidad"
              className="text-muted-foreground hover:text-primary px-2 py-2 text-sm"
            >
              Privacidad
            </Link>
            <Link
              href="/cookies"
              className="text-muted-foreground hover:text-primary px-2 py-2 text-sm"
            >
              Cookies
            </Link>
          </div>
        </div>
      </div>
    </footer>
  );
}
